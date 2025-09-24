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
    private readonly string jwtKey = "tavo_labai_slaptas_raktas"; // kolkas demo tiesiog

    [HttpPost("register")] //endpointas registracijai
    public IActionResult Register([FromBody] User? user)
    {
        if (user == null)
            return BadRequest("User data is required");

        if (UserStore.Users.Any(u => u.Email == user.Email))
            return BadRequest("User already exists");

        user.Id = UserStore.Users.Count > 0 ? UserStore.Users.Max(u => u.Id) + 1 : 1; //suteikiam id
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

        UserStore.Users.Add(user);
        return Ok("User registered successfully");
    }

    [HttpPost("login")] 
    public IActionResult Login([FromBody] User? user)
    {
        if (user == null)
            return BadRequest("User data is required");

        var existingUser = UserStore.Users.FirstOrDefault(u => u.Email == user.Email);
        if (existingUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password)) //verify tikrina hasha ar slaptazodis teisingas
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(existingUser);
        return Ok(new { token }); //sukuria ir grazina prisijungimo bilieta, kad serveris paskui zinotu kad autentifikuotas zmogus 
    }

    [HttpPost("google-signin")]
    public async Task<IActionResult> GoogleSignIn([FromBody] string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken); //tikrina ar google tokenas tikras

            var user = UserStore.Users.FirstOrDefault(u => u.Email == payload.Email); //jei tokio nera tai sukuriam
            if (user == null)
            {
                user = new User
                {
                    Id = UserStore.Users.Count > 0 ? UserStore.Users.Max(u => u.Id) + 1 : 1,
                    Email = payload.Email
                };
                UserStore.Users.Add(user);
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }
        catch
        {
            return BadRequest("Invalid Google token");
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
