using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Data.SqlClient;


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        private readonly string _connString;

        public LobbyController(IConfiguration configuration)
        {
            //Console.WriteLine(configuration.GetConnectionString("ConnectionString"));
            _connString = configuration.GetConnectionString("DefaultConnection");
        }
        
        //[Authorize] // tik prisijungę per Google gali kurti lobby
        [HttpPost("create")]
        public IActionResult CreateLobby([FromBody] Lobby? options)
        {
            if (options == null)
                return BadRequest("Options required");

            if (options.Token == "")
            {
                return Unauthorized("Token required");
            }
            
            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }

            //Paimam tokena
            var token = options.Token;
            
            //Sql connectionas
            using var conn = new SqlConnection(_connString);
            conn.Open();
            
            //Patikrinam ar tokenas valid
            var tokenQuery = "SELECT 1 FROM users WHERE gid = @token";
            using var tokenCmd = new SqlCommand(tokenQuery, conn);
            tokenCmd.Parameters.AddWithValue("@token", token);

            if (tokenCmd.ExecuteScalar() == null)
            {
                return Unauthorized("User authentication failed");
            }
            
            Random rnd = new Random();
            var lobCode = 0;
            
            

            var newLobby = new Lobby
            {
                Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                Private = options.Private, //is frontendo gaunamos reiksmes
                AiRate = options.AiRate,
                HumanRate = options.HumanRate,
                LobbyCode = lobCode
            };
            
            newLobby.PlayersTokens.Add(token);

            while (true)
            {
                lobCode = rnd.Next(1000, 9999);
                var lobcQuery = "SELECT 1 FROM lobby WHERE lobbyCode = @lobCode";
                using var lobCmd = new SqlCommand(lobcQuery, conn);
                lobCmd.Parameters.AddWithValue("@lobCode", lobCode);

                if (lobCmd.ExecuteScalar() == null)
                {
                    break;
                }
            }

            //Issaugom lobby sql
            var createQuery = "INSERT INTO lobby (lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players) VALUES(@lobbyCode, @isPrivate, @isAiRate, @isHumanRate, @creator, @players)";
            using var createCmd = new SqlCommand(createQuery, conn);
            createCmd.Parameters.AddWithValue("@lobbyCode", lobCode);
            createCmd.Parameters.AddWithValue("@isPrivate", newLobby.Private);
            createCmd.Parameters.AddWithValue("@isAiRate", newLobby.AiRate);
            createCmd.Parameters.AddWithValue("@isHumanRate", newLobby.HumanRate);
            createCmd.Parameters.AddWithValue("@creator", token);
            createCmd.Parameters.AddWithValue("@players", token);


            createCmd.ExecuteScalar();
            //LobbyStore.Lobbies.Add(newLobby);

            return Ok(lobCode);
        }

        //[Authorize]
        [HttpPost("play")]
        public IActionResult Play([FromBody] PlayRequest request)
        {
            var token = request.Token;

            //Patikrinam Tokena
            if (token == "")
                return Unauthorized("User not found");
            
            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }
            
            //Sql connectionas
            using var conn = new SqlConnection(_connString);
            conn.Open();
            
            //Patikrinam ar tokenas valid
            var tokenQuery = "SELECT 1 FROM users WHERE gid = @token";
            using var tokenCmd = new SqlCommand(tokenQuery, conn);
            tokenCmd.Parameters.AddWithValue("@token", token);

            if (tokenCmd.ExecuteScalar() == null)
            {
                return Unauthorized("User authentication failed");
            }
            

            if (!string.IsNullOrEmpty(request.LobbyCode)) //jeigu ne null tai iveda seed
            {
                var lobcQuery = "SELECT COUNT(players) FROM lobby WHERE lobbyCode = @lobcode";
                using var lobcCmd = new SqlCommand(lobcQuery, conn);
                lobcCmd.Parameters.AddWithValue("@lobcode", request.LobbyCode);
                var playerCount = Int32.Parse(lobcCmd.ExecuteScalar().ToString());
                if (playerCount == null)
                {
                    return NotFound("Lobby not found");
                }

                if (playerCount >= Lobby.MaxPlayers)
                {
                    return BadRequest("Lobby limit exceeded");
                }
                
                var lobQuery = "SELECT id, lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players FROM lobby WHERE lobbyCode = @lobcode";
                var lobcmd = new SqlCommand(lobQuery, conn);
                lobcmd.Parameters.AddWithValue("@lobcode", request.LobbyCode);
                var reader =  lobcmd.ExecuteReader();
                
                Lobby lobby = new Lobby();
                if (reader.Read())
                {
                    lobby.Id = reader.GetInt32(reader.GetOrdinal("id"));
                    lobby.LobbyCode = reader.GetInt32(reader.GetOrdinal("lobbyCode"));
                    lobby.Private = reader.GetInt32(reader.GetOrdinal("isPrivate")).Equals(1);
                    lobby.AiRate = reader.GetInt32(reader.GetOrdinal("isAiRate")).Equals(1);
                    lobby.HumanRate = reader.GetInt32(reader.GetOrdinal("isHumanRate")).Equals(1);
                    lobby.Creator = reader.GetString(reader.GetOrdinal("creator"));
                    var players = reader.GetString(reader.GetOrdinal("players"));
                    lobby.PlayersTokens = (players.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();
                }
                reader.Close();
                
                lobby.PlayersTokens.Add(token);
                
                var insertJoinQuery = "UPDATE lobby SET players = @players WHERE id = @id";
                var insertJoinCmd = new SqlCommand(insertJoinQuery, conn);
                var playersJoinStr = string.Join(",", lobby.PlayersTokens);
                insertJoinCmd.Parameters.AddWithValue("@players", playersJoinStr);
                insertJoinCmd.Parameters.AddWithValue("@id", lobby.Id);
                insertJoinCmd.ExecuteScalar();

                return Ok(lobby);
            }
            
            var findLobbyQuery = "SELECT TOP 1 id, lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players " +
                                 "FROM lobby WHERE LEN(players) - LEN(REPLACE(players, ',', '')) + 1 < @maxPlayers";
            var findLobbyCmd = new SqlCommand(findLobbyQuery, conn);
            findLobbyCmd.Parameters.AddWithValue("@maxPlayers", Lobby.MaxPlayers);
            var lobReader = findLobbyCmd.ExecuteReader();

            Lobby lobbyRand = new Lobby();
            if (lobReader.Read())
            {
                lobbyRand.Id = lobReader.GetInt32(lobReader.GetOrdinal("id"));
                lobbyRand.LobbyCode = lobReader.GetInt32(lobReader.GetOrdinal("lobbyCode"));
                lobbyRand.Private = lobReader.GetInt32(lobReader.GetOrdinal("isPrivate")).Equals(1);
                lobbyRand.AiRate = lobReader.GetInt32(lobReader.GetOrdinal("isAiRate")).Equals(1);
                lobbyRand.HumanRate = lobReader.GetInt32(lobReader.GetOrdinal("isHumanRate")).Equals(1);
                lobbyRand.Creator = lobReader.GetString(lobReader.GetOrdinal("creator"));
                lobbyRand.PlayersTokens = lobReader.GetString(lobReader.GetOrdinal("players")).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            lobReader.Close();

            if (lobbyRand.Id == 0)
            {
                return NotFound("Empty lobby not found");// TODO: Frontend should react to this response and show user a create lobby screen
            }
            
            if (!lobbyRand.PlayersTokens.Contains(token))
            {
                lobbyRand.PlayersTokens.Add(token);
                var updateQuery = "UPDATE lobby SET players = @players WHERE id = @id";
                using var updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@players", string.Join(",", lobbyRand.PlayersTokens));
                updateCmd.Parameters.AddWithValue("@id", lobbyRand.Id);
                updateCmd.ExecuteNonQuery();
            }

            return Ok(lobbyRand);
        }
    }
}
