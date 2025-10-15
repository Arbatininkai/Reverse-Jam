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
                return BadRequest("Token required");
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
            string tokenQuery = "SELECT 1 FROM users WHERE gid = @token";
            using var tokenCmd = new SqlCommand(tokenQuery, conn);
            tokenCmd.Parameters.AddWithValue("@token", token);

            if (tokenCmd.ExecuteScalar() == null)
            {
                return BadRequest("User authentication failed");
            }
            
            Random rnd = new Random();
            int lobCode = 0;
            
            

            var newLobby = new Lobby
            {
                Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                Private = options.Private, //is frontendo gaunamos reiksmes
                AiRate = options.AiRate,
                HumanRate = options.HumanRate,
                LobbyCode = lobCode
            };
            
            newLobby.Players.Add(token);

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
            createCmd.Parameters.AddWithValue("@creator", token);
            createCmd.Parameters.AddWithValue("@players", token);


            createCmd.ExecuteScalar();
            //LobbyStore.Lobbies.Add(newLobby);

            return Ok(lobCode);
        }
    }
}
