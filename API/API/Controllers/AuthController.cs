using API.Models;
using API.Stores;
using Google.Apis.Auth; //google tokenui
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens; //jwt
using System.IdentityModel.Tokens.Jwt; //jwt
using System.Security.Claims; //vartotojo duomenims i tokena
using System.Text; //tekstu kodavimui (raktas i baitus)

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")] //url bus api/auth
public class AuthController : ControllerBase
{
    private readonly string jwtKey = "tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!"; // kolkas demo tiesiog
    private static async Task<IEnumerable<SecurityKey>> GetGoogleKeysAsync()
    {
        using var http = new HttpClient();
        var json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs"); //gauna viesus google raktus
        var keys = new JsonWebKeySet(json);
        return keys.GetSigningKeys(); //grazina raktus, kuriais galima patikrinti JWT parasa 
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
                ValidAudience = "945939078641-no1bls6nnf2s5teqk3m5b1q3kfkorle1.apps.googleusercontent.com", //google client ID
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKeys = await GetGoogleKeysAsync()
            };

            tokenHandler.ValidateToken(idToken, validationParameters, out var validatedToken); //patikrina ar gautas idToken yra tinkamas google token

            var jwtToken = (JwtSecurityToken)validatedToken;
            var email = jwtToken.Claims.First(c => c.Type == "email").Value; //gauna email
            var name = jwtToken.Claims.First(c => c.Type == "name").Value; //gauna varda
            var photoUrl = jwtToken.Claims.First(c => c.Type == "picture").Value; //gauna nuotrauka

            var user = UserStore.Users.FirstOrDefault(u => u.Email == email); //jei tokio nera tai sukuriam
            if (user == null)
            {
                user = new User
                {
                    Id = UserStore.Users.Count > 0 ? UserStore.Users.Max(u => u.Id) + 1 : 1,
                    Email = email,
                    Name = name,
                    PholoUrl = photoUrl,
                };
                UserStore.Users.Add(user);
            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user
            });
        }
        catch
        {
            return BadRequest(new { message = "Invalid Google token" });
        }
    }
    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtKey); //pavercia i baitus

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? "")

            }),
            Expires = DateTime.UtcNow.AddHours(1), //nustatom kiek laiko galios tokenas
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token); //sukuria galutini jwt stringa
    }

    [Authorize] //leidzia vykdyt dalykus jeigu yra galiojantis jwt
    [HttpGet("me")] // patikrina prisijungima
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { userId, email });
    }
}
