using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Data;
using Google.Apis.Auth;
using API.Stores;


namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly string jwtKey = "tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!";
    private readonly AppDbContext _context;

   
    private readonly IUserStore _userStore;
    public AuthController(IUserStore userStore, AppDbContext context)
    {
        
        _userStore = userStore;
        _context = context;
    }

    private static async Task<IEnumerable<SecurityKey>> GetGoogleKeysAsync()
    {
        using var http = new HttpClient();
        var json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs");
        var keys = new JsonWebKeySet(json);
        return keys.GetSigningKeys();
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
                ValidAudience = "945939078641-no1bls6nnf2s5teqk3m5b1q3kfkorle1.apps.googleusercontent.com",
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKeys = await GetGoogleKeysAsync()
            };

            tokenHandler.ValidateToken(idToken, validationParameters, out var validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;

            var email = jwtToken.Claims.First(c => c.Type == "email").Value;
            var name = jwtToken.Claims.First(c => c.Type == "name").Value;
            var photoUrl = jwtToken.Claims.First(c => c.Type == "picture").Value;

            // Check if user exists in the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Create a new user
                user = new User
                {
                    Email = email,
                    Name = name,
                    PhotoUrl = photoUrl,
                    Emoji = "1F600"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update user info if changed
                bool changed = false;
                if (user.Name != name)
                {
                    user.Name = name; changed = true;
                }

                if (user.PhotoUrl != photoUrl)
                {
                    user.PhotoUrl = photoUrl; changed = true;
                }

                if (changed)
                    await _context.SaveChangesAsync();
            }
            
            if (!_userStore.Users.Any(u => u.Id == user.Id))
            {
                _userStore.Users.Add(user);
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token, user });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Invalid Google token: " + ex.Message });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Authorize]
    [HttpGet("userinfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        try
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { message = "User email missing in token" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error fetching user info", error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { userId, email });
    }
}
