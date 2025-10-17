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
    public class PlayerController : ControllerBase
    {
        private readonly string _connString;

        public PlayerController(IConfiguration configuration)
        {
            //Console.WriteLine(configuration.GetConnectionString("ConnectionString"));
            _connString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("change-name")]
        public IActionResult ChangeName([FromBody] User? options)
        {
            if (options == null)
            {
                return BadRequest("Options required");
            }
            
            if (options.Token == "")
            {
                return Unauthorized("Token required");
            }
            
            if (string.IsNullOrEmpty(_connString))
            {
                return BadRequest("Connection string is missing!");
            }
            
            var token = options.Token;
            
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
            
            var name = options.Name;
            var emoji = options.Emoji;
            
            var updateQuery = "UPDATE users SET name = @name, emoji = @emoji WHERE gid = @id";
            using var updateCmd = new SqlCommand(updateQuery, conn);
            updateCmd.Parameters.AddWithValue("@name", name);
            updateCmd.Parameters.AddWithValue("@emoji", emoji);
            updateCmd.Parameters.AddWithValue("@id", token);
            updateCmd.ExecuteNonQuery();
            return Ok();
        }
    }
};