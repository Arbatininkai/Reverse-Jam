using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using API.Data;
using API.Models;

[Collection(nameof(DatabaseTestCollection))]
public class AuthControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;

    public AuthControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }
    
    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // -------------------- TESTS --------------------

    [Fact]
    public async Task Me_NoToken_Returns401()
    {
        // ARRANGE
        var client = _factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/api/auth/me");

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserInfo_UserExists_ReturnsData()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new User { Name = "Test", Email = "test@test.com" };
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        
        var jwt = GenerateFakeJwt(user.Id, user.Email);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        // ACT
        var response = await client.GetAsync("/api/auth/userinfo");

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("test@test.com", body);
    }

    [Fact]
    public async Task GoogleSignIn_InvalidToken_Returns400()
    {
        // ARRANGE
        var client = _factory.CreateClient();
        var content = new StringContent("\"invalid_token\"", Encoding.UTF8, "application/json");

        // ACT
        var response = await client.PostAsync("/api/auth/google-signin", content);

        // ASSERT
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    
    private string GenerateFakeJwt(int id, string email)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!");

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, id.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, email)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
