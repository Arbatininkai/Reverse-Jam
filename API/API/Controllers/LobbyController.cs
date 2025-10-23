using API.Hubs;
using API.Models;
//using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

using Google.Apis.Auth;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        private readonly string _connString;
        private readonly IHubContext<LobbyHub> _hubContext;

        public LobbyController(IHubContext<LobbyHub> hubContext, IConfiguration configuration)
        {
             _connString = configuration.GetConnectionString("DefaultConnection");
            _hubContext = hubContext;
        }

        [Authorize] // tik prisijungę per Google gali kurti lobby
        [HttpPost("create")]
        public IActionResult CreateLobby([FromBody] Lobby? options)
        {
            if (options == null)
            {
                return BadRequest("Options required");
            }

            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }
            
            
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            if (userEmail == null)
            {
                return BadRequest("Authentication token is missing!");
            }
            
            //Sql connection initiate
            using var conn = new SqlConnection(_connString);
            conn.Open();
            
            //Query to select userID from email
            var idQuery = "SELECT id FROM users WHERE email = @email";
            using var idCmd = new SqlCommand(idQuery, conn);
            idCmd.Parameters.AddWithValue("@email", userEmail);

            string userID = null;
            object tempID = idCmd.ExecuteScalar();
            
            if (tempID != null)
            {
                userID = Convert.ToString(tempID);
            }
            
            var maxIdQuery = "SELECT MAX(id) + 1 FROM lobby";
            using var maxIdCmd = new SqlCommand(maxIdQuery, conn);
            var maxId = (Int32)(maxIdCmd.ExecuteScalar());
            
            
            var newLobby = new Lobby
            {
                Id = maxId,
                Private = options.Private, //is frontendo gaunamos reiksmes
                AiRate = options.AiRate,
                HumanRate = options.HumanRate,
                OwnerId = Int32.Parse(userID)
            };

            User player = new User(Int32.Parse(userID), userEmail, userName);
            
            newLobby.Players.Add(player);

            Random rnd = new Random();
            int lobCode = 0;
            while (true)
            {
                lobCode = rnd.Next(1000, 9999);
                string lobcQuery = "SELECT 1 FROM lobby WHERE lobbyCode = @lobCode";
                using var lobCmd = new SqlCommand(lobcQuery, conn);
                lobCmd.Parameters.AddWithValue("@lobCode", lobCode);

                if (lobCmd.ExecuteScalar() == null)
                {
                    break;
                }
            }
            
            //Issaugom lobby sql
            string createQuery = "INSERT INTO lobby (lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players) VALUES(@lobbyCode, @isPrivate, @isAiRate, @isHumanRate, @creator, @players)";
            using var createCmd = new SqlCommand(createQuery, conn);
            createCmd.Parameters.AddWithValue("@lobbyCode", lobCode);
            createCmd.Parameters.AddWithValue("@isPrivate", newLobby.Private);
            createCmd.Parameters.AddWithValue("@isAiRate", newLobby.AiRate);
            createCmd.Parameters.AddWithValue("@isHumanRate", newLobby.HumanRate);
            createCmd.Parameters.AddWithValue("@creator", userID);
            createCmd.Parameters.AddWithValue("@players", userID);
            createCmd.ExecuteScalar();
            
            newLobby.LobbyCode = lobCode;
            //LobbyStore.Lobbies.Add(newLobby);

            conn.Close();
            return Ok(newLobby);//CHANGEME: lobCode buvo bent
        }

        [HttpGet("exists/{code}")]
        public IActionResult LobbyExists(string code)
        {
            if (code == null)
            {
                return BadRequest("Invalid code");
            }
            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }
            
            using var conn = new SqlConnection(_connString);
            conn.Open();
            var exstQuery = "SELECT 1 FROM lobby WHERE lobbyCode = @code";
            using var exstCmd = new SqlCommand(exstQuery, conn);
            exstCmd.Parameters.AddWithValue("@code", code);
            if (exstCmd.ExecuteScalar() != null)
            {
                return Ok();
            }
            else
            {
                return NotFound("Lobby not found");
            }
        }


        [Authorize]
        [HttpPost("play")]
        public IActionResult Play([FromBody] PlayRequest request)
        {
            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }
            
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            if (userEmail == null)
            {
                return BadRequest("Authentication token is missing!");
            }
            
            using var conn = new SqlConnection(_connString);
            conn.Open();
            
            var idQuery = "SELECT id FROM users WHERE email = @email";
            using var idCmd = new SqlCommand(idQuery, conn);
            idCmd.Parameters.AddWithValue("@email", userEmail);
            string userID = null;
            object tempID = idCmd.ExecuteScalar();
            if (tempID != null)
            {
                userID = Convert.ToString(tempID);
            }
            
            if (!string.IsNullOrEmpty(request.LobbyCode)) //Seed is entered
            {
                var lobcQuery = "SELECT COUNT(players) FROM lobby WHERE lobbyCode = @lobcode";
                using var lobcCmd = new SqlCommand(lobcQuery, conn);
                lobcCmd.Parameters.AddWithValue("@lobcode", request.LobbyCode);
                var playerCount = Int32.Parse(lobcCmd.ExecuteScalar().ToString());
                if (playerCount == null)
                {
                    conn.Close();
                    return NotFound("Lobby not found");
                }
                if (playerCount >= Lobby.MaxPlayers)
                {
                    conn.Close();
                    return BadRequest("Lobby limit exceeded");
                }
                var lobQuery = "SELECT id, lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players FROM lobby WHERE lobbyCode = @lobcode";
                var lobcmd = new SqlCommand(lobQuery, conn);
                lobcmd.Parameters.AddWithValue("@lobcode", request.LobbyCode);
                var reader =  lobcmd.ExecuteReader();

                Lobby lobby = new Lobby();
                var allPlayersIds = "";
                if (reader.Read())
                {
                    lobby.Id = reader.GetInt32(reader.GetOrdinal("id"));
                    lobby.LobbyCode = reader.GetInt32(reader.GetOrdinal("lobbyCode"));
                    lobby.Private = reader.GetInt32(reader.GetOrdinal("isPrivate")).Equals(1);
                    lobby.AiRate = reader.GetInt32(reader.GetOrdinal("isAiRate")).Equals(1);
                    lobby.HumanRate = reader.GetInt32(reader.GetOrdinal("isHumanRate")).Equals(1);
                    lobby.OwnerId = Int32.Parse(reader.GetString(reader.GetOrdinal("creator")));
                    allPlayersIds = reader.GetString(reader.GetOrdinal("players"));
                }
                reader.Close();

                if (allPlayersIds.Contains(userID))
                {
                    conn.Close();
                    return Ok("Player is already playing");
                }
                allPlayersIds += ", " + userID;//TODO: pasidomet ar veikia toks prikolas
                
                foreach (var player in allPlayersIds.Split(','))
                {
                    var userQuery = "SELECT id, email, name, photoUrl FROM players WHERE id = @id";
                    var userCmd = new SqlCommand(userQuery, conn);
                    userCmd.Parameters.AddWithValue("@id", player);
                    var userReader =  userCmd.ExecuteReader();
                    if (userReader.Read())
                    {
                        var user = new User(
                            id: userReader.GetInt32(userReader.GetOrdinal("id")),
                            email: userReader.GetString(userReader.GetOrdinal("email")),
                            name: userReader.GetString(userReader.GetOrdinal("name")),
                            photoUrl: userReader.GetString(userReader.GetOrdinal("photoUrl"))
                            );
                        lobby.Players.Add(user);
                    }
                }
                

                var insertJoinQuery = "UPDATE lobby SET players = @players WHERE id = @id";
                var insertJoinCmd = new SqlCommand(insertJoinQuery, conn);
                //var playersJoinStr = string.Join(",", lobby.PlayersIds);
                insertJoinCmd.Parameters.AddWithValue("@players", allPlayersIds);
                insertJoinCmd.Parameters.AddWithValue("@id", lobby.Id);
                insertJoinCmd.ExecuteScalar();
                
                conn.Close();
                return Ok(lobby);

            }

            
            
            //Finding random lobby
            var findLobbyQuery = "SELECT TOP 1 id, lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players " +
                                 "FROM lobby WHERE LEN(players) - LEN(REPLACE(players, ',', '')) + 1 < @maxPlayers";
            var findLobbyCmd = new SqlCommand(findLobbyQuery, conn);
            findLobbyCmd.Parameters.AddWithValue("@maxPlayers", 4);
            var lobReader = findLobbyCmd.ExecuteReader();

            Lobby lobbyRand = new Lobby
            {
                Id = 0
            };
            var allPlayersId = "";
            if (lobReader.Read())
            {
                lobbyRand.Id = lobReader.GetInt32(lobReader.GetOrdinal("id"));
                lobbyRand.LobbyCode = lobReader.GetInt32(lobReader.GetOrdinal("lobbyCode"));
                lobbyRand.Private = lobReader.GetInt32(lobReader.GetOrdinal("isPrivate")).Equals(1);
                lobbyRand.AiRate = lobReader.GetInt32(lobReader.GetOrdinal("isAiRate")).Equals(1);
                lobbyRand.HumanRate = lobReader.GetInt32(lobReader.GetOrdinal("isHumanRate")).Equals(1);
                lobbyRand.OwnerId = Int32.Parse(lobReader.GetString(lobReader.GetOrdinal("creator")));
                allPlayersId = lobReader.GetString(lobReader.GetOrdinal("players"));
            }
            lobReader.Close();
            
            if (lobbyRand.Id == 0)
            {
                conn.Close();
                return NotFound("Empty lobby not found");// TODO: Frontend should react to this response and show user a create lobby screen
            }
            
            if (allPlayersId.Contains(userID))
            {
                conn.Close();
                return Ok("Player is already playing");
            }
            allPlayersId += ", " + userID;//TODO: pasidomet ar veikia toks prikolas
            
            foreach (var player in allPlayersId.Split(','))
            {
                var userQuery = "SELECT id, email, name, photoUrl FROM players WHERE id = @id";
                var userCmd = new SqlCommand(userQuery, conn);
                userCmd.Parameters.AddWithValue("@id", player);
                var userReader =  userCmd.ExecuteReader();
                if (userReader.Read())
                {
                    var user = new User(
                        id: userReader.GetInt32(userReader.GetOrdinal("id")),
                        email: userReader.GetString(userReader.GetOrdinal("email")),
                        name: userReader.GetString(userReader.GetOrdinal("name")),
                        photoUrl: userReader.GetString(userReader.GetOrdinal("photoUrl"))
                    );
                    lobbyRand.Players.Add(user);
                }
            }
            
            var updateQuery = "UPDATE lobby SET players = @players WHERE id = @id";
            using var updateCmd = new SqlCommand(updateQuery, conn);
            updateCmd.Parameters.AddWithValue("@players", allPlayersId);
            updateCmd.Parameters.AddWithValue("@id", lobbyRand.Id);
            updateCmd.ExecuteNonQuery();
            
            conn.Close();
            return Ok(lobbyRand);
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLobby([FromBody] int lobbyId)
        {
            if (lobbyId == null)
            {
                return BadRequest("Options required");
            }

            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }
            
            
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            if (userEmail == null)
            {
                return BadRequest("Authentication token is missing!");
            }
            
            //Sql connection initiate
            using var conn = new SqlConnection(_connString);
            conn.Open();
            
            
            var authQuery = "SELECT lobbyCode FROM lobby WHERE id = @id";
            using var authCmd = new SqlCommand(authQuery, conn);
            authCmd.Parameters.AddWithValue("@id", lobbyId);
            var lobbyCode = authCmd.ExecuteScalar();
            
            var delQuery = "DELETE FROM lobby WHERE lobbyCode = @lobCode";
            using var delCmd = new SqlCommand(delQuery, conn);
            delCmd.Parameters.AddWithValue("@lobCode", Convert.ToInt32(lobbyCode));
            delCmd.ExecuteNonQuery();
            conn.Close();
            //
            await _hubContext.Clients.Group(lobbyCode.ToString()).SendAsync("LobbyDeleted");
            return Ok("Lobby has been deleted");
            
            /*var lobby = LobbyStore.Lobbies.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found");

            // Notify all players that the lobby is deleted
            await _hubContext.Clients.Group(lobby.LobbyCode.ToString()).SendAsync("LobbyDeleted");

            // Remove the lobby
            LobbyStore.Lobbies.Remove(lobby);

            return Ok(new { message = "Lobby deleted successfully" });*/
        }
    }
}
