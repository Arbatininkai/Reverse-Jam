using Integrations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Services.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly string _jwtKey = "tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!";
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        private static async Task<IEnumerable<SecurityKey>> GetGoogleKeysAsync()
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v3/certs");
            var keys = new JsonWebKeySet(json);
            return keys.GetSigningKeys();
        }

        public async Task<AuthResponse> GoogleSignInAsync(string idToken)
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new UserEntity
                {
                    Email = email,
                    Name = name,
                    PhotoUrl = photoUrl,
                    Emoji = "1F600",
                    TotalWins = 0
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            var model = new UserDto
            {
                Id = user.Id,
                Name = user.Name ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhotoUrl = user.PhotoUrl ?? string.Empty,
                Emoji = user.Emoji ?? string.Empty,
                TotalWins = user.TotalWins
            };

            return new AuthResponse
            {
                User = model,
                Token = GenerateJwtToken(model)
            };
        }

        private string GenerateJwtToken(UserDto user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtKey);

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

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var entity = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
            if (entity == null) return null;

            return new UserDto
            {
                Id = entity.Id,
                Name = entity.Name ?? string.Empty,
                Email = entity.Email ?? string.Empty,
                PhotoUrl = entity.PhotoUrl ?? string.Empty,
                Emoji = entity.Emoji ?? string.Empty,
                TotalWins = entity.TotalWins
            };
        }
    }
}

