using API.Models;
using API.Stores;
using Google.Apis.Auth; //google tokenui
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens; //jwt
using System.IdentityModel.Tokens.Jwt; //jwt
using System.Security.Claims; //vartotojo duomenims i tokena
using System.Text; //tekstu kodavimui (raktas i baitus)
//sql
using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Data.SqlClient;


namespace API.Controllers;

[ApiController]
[Route("api/[controller]")] //url api/auth
public class AuthController : ControllerBase
{
    private readonly string jwtKey = "tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!"; // kolkas demo tiesiog
    private readonly string _connectionString;

    public AuthController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    private static async Task<IEnumerable<SecurityKey>> GetGoogleKeysAsync()
    {
        using var http = new HttpClient();
        var json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs"); //Gets public google keys
        var keys = new JsonWebKeySet(json);
        return keys.GetSigningKeys(); //Return keys which lets us check JWT
    }

    [HttpPost("google-signin")]
    public async Task<IActionResult> GoogleSignIn([FromBody] string idToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = "https://accounts.google.com",
                ValidAudience =
                    "945939078641-no1bls6nnf2s5teqk3m5b1q3kfkorle1.apps.googleusercontent.com", //google client ID
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKeys = await GetGoogleKeysAsync()
            };


            //Check if idToken is a valid token
            tokenHandler.ValidateToken(idToken, validationParameters, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var email = jwtToken.Claims.First(c => c.Type == "email").Value;
            var name = jwtToken.Claims.First(c => c.Type == "name").Value;
            var photoUrl = jwtToken.Claims.First(c => c.Type == "picture").Value;

            //Opening sql connection
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            //Querying sql to check is there a user created with this email
            var query = "SELECT COUNT(*) FROM users WHERE email = @email";
            var parameters = new Dictionary<string, object>
            {
                { "@email", email },
            };
            object result = SqlQuery(query, conn, parameters);
            int count = (int)result; //Unboxing

            var user = UserStore.Users.FirstOrDefault(u => u.Email == email);
            //If user doesn't exist we need to register it
            if (count == 0)
            {
                user = new User
                {
                    Id = UserStore.Users.Count > 0 ? UserStore.Users.Max(u => u.Id) + 1 : 1,
                    Email = email,
                    Name = name,
                    PhotoUrl = photoUrl,
                };
                UserStore.Users.Add(user);
                query = "INSERT INTO users(email, name, photourl, id) VALUES(@email, @name, @photourl, @id)";
                parameters = new Dictionary<string, object>
                {
                    { "@email", user.Email },
                    { "@name", user.Name },
                    { "@photourl", user.PhotoUrl },
                    { "@id", user.Id }
                };
                SqlQuery(query, conn, parameters);

            }

            //If user exist return it's info from database
            else
            {
                query = "SELECT TOP 1 CONCAT(id, ';', email, ';', name, ';', photourl) FROM users WHERE email = @email";
                parameters = new Dictionary<string, object>
                {
                    { "@email", email},
                };

                result = SqlQuery(query, conn, parameters);

                //Do this because the result has multiple variables
                string combined = result.ToString() ?? "";
                var parts = combined.Split(';');

                user = new User
                {
                    Id = int.Parse(parts[0]),
                    Email = parts[1],
                    Name = parts[2],
                    PhotoUrl = parts[3]
                };

                if (!UserStore.Users.Any(u => u.Email == email))
                    UserStore.Users.Add(user);

            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Invalid Google token" + ex.Message });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey); //Convert to bytes

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? "")

            }),
            Expires = DateTime.UtcNow.AddDays(7), //After how many days the token should expire
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token); //Generate final JWT string
    }

    [Authorize]
    [HttpGet("userinfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        try
        {
            //Get information from JWT token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID missing in token" });
            }

            //Open sql connection
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            //Select user information
            string query = "SELECT CONCAT(id, ';', email, ';', name, ';', photourl) FROM users WHERE email = @Email";
            var parameters = new Dictionary<string, object>
            {
                { "@email", email},
            };
            object result = SqlQuery(connection: conn, query: query, parameters: parameters); // Named arguments

            object emptyObj = ""; // Boxing
            if (result == emptyObj)
            {
                return Unauthorized(new { message = "User not found" });
            }

            string combined = result.ToString() ?? "";
            var parts = combined.Split(';');

            //Store information inside user object
            var user = new User
            {
                Id = int.Parse(parts[0]),
                Email = parts[1],
                Name = parts[2],
                PhotoUrl = parts[3]
            };

            // Make sure UserStore has latest data
            var existing = UserStore.Users.FirstOrDefault(u => u.Id == user.Id);
            if (existing != null)
            {
                existing.Email = user.Email;
                existing.Name = user.Name;
                existing.PhotoUrl = user.PhotoUrl;
            }
            else
            {
                UserStore.Users.Add(user);
            }

            return Ok(user);

        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error fetching user info", error = ex.Message });
        }
    }


    [Authorize]
    [HttpGet("me")] // Check if user is logged in
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { userId, email });
    }

    private object SqlQuery(string query, SqlConnection connection, Dictionary<string, object>? parameters = null)
    {
        using var cmd = new SqlCommand(query, connection);

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }

        object result = cmd.ExecuteScalar();
        if (result == null)
            return "ERROR | No result";
        return result;
    }
}