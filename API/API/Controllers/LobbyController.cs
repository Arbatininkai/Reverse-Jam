using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Data.SqlClient;

using System.Text.Json;

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

            
            //TODO: Pakeisti su sql random gavima
            var randLobQuery = "SELECT id, lobbyCode, isPrivate, isAiRate, isHumanRate, creator, players FROM lobby WHERE players < 4";
            var randLobCmd = new SqlCommand(randLobQuery, conn);
            
            var Lobreader =  randLobCmd.ExecuteReader();
            Lobby randLobby = new Lobby();
            if (Lobreader.Read())
            {
                randLobby.Id = Lobreader.GetInt32(Lobreader.GetOrdinal("id"));
                randLobby.LobbyCode = Lobreader.GetInt32(Lobreader.GetOrdinal("lobbyCode"));
                randLobby.Private = Lobreader.GetBoolean(Lobreader.GetOrdinal("isPrivate")).Equals(1);
                randLobby.AiRate = Lobreader.GetBoolean(Lobreader.GetOrdinal("isAiRate")).Equals(1);
                randLobby.HumanRate = Lobreader.GetBoolean(Lobreader.GetOrdinal("isHumanRate")).Equals(1);
                randLobby.Creator = Lobreader.GetString(Lobreader.GetOrdinal("creator"));
                var players = Lobreader.GetString(Lobreader.GetOrdinal("players")); 
                randLobby.PlayersTokens =  (players.Split(',', StringSplitOptions.RemoveEmptyEntries)).ToList();
            }
            Lobreader.Close();
            
            randLobby.PlayersTokens.Add(token);
            
            var insertRandQuery = "UPDATE lobby SET players = @players WHERE id = @id";
            var insertRandCmd = new SqlCommand(insertRandQuery, conn);
            var playersRandStr = string.Join(",", randLobby.PlayersTokens);
            insertRandCmd.Parameters.AddWithValue("@players", playersRandStr);
            insertRandCmd.Parameters.AddWithValue("@id", randLobby.Id);
            insertRandCmd.ExecuteScalar();
            
            return Ok(randLobby);
            //TODO: Padaryti kad tas pats zmogus negaletu joininti i ta pati lobby 2 kartus t.y. returninti lobby bet neupdeitinti db
            
            
            //TODO: Sugalvoti kaip padaryti sita
            /*if (availableLobbies.Count == 0)
            {
                var newLobby = new Lobby
                {
                    Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                    Private = false,
                    MaxPlayers = 4
                };
                newLobby.Players.Add(user);
                LobbyStore.Lobbies.Add(newLobby);
                return Ok(newLobby);
            }

            int maxPlayersNow = availableLobbies.Max(l => l.Players.Count);
            var bestLobbies = availableLobbies.Where(l => l.Players.Count == maxPlayersNow).ToList(); //randam labiausiai uzpildyta lobby

            var random = new Random();
            var chosenLobby = bestLobbies[random.Next(bestLobbies.Count)]; //isrenkam random jei yra keli

            if (!chosenLobby.Players.Any(p => p.Id == user.Id)) //apsauga jeigu tas pats useris bando i ta pati lobby eiti
                chosenLobby.Players.Add(user);

            return Ok(chosenLobby);*/
        }
    }
}
