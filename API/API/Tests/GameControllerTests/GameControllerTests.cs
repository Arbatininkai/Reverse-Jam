using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using API.Data;
using API.Models;
using API.Stores;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(DatabaseTestCollection))]
public class GameControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;

    public GameControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    
    private string GenerateFakeJwt(int userId, string email)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!");

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()),
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


    [Fact]
    public async Task SubmitVotes_MissingLobbyCode_ShouldReturnBadRequest()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new User { Email = "test@test.com", Name = "TestUser" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        var requestObj = new { Votes = new object(), Round = 1, LobbyCode = "" };
        var content = new StringContent(JsonConvert.SerializeObject(requestObj), Encoding.UTF8, "application/json");

        // ACT
        var response = await client.PostAsync("/api/game/submit-votes", content);

        // ASSERT
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }



    [Fact]
    public async Task SubmitVotes_ValidLobby_ShouldReturnOK()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();

        var user = new User { Email = "test@test.com", Name = "TestUser" };
        var lobby = new Lobby { LobbyCode = "GAME123", Players = new List<User> { user } };
        db.Users.Add(user);
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var requestObj = new { Votes = new[] { new { UserId = user.Id, Score = 5 } }, Round = 1, LobbyCode = "GAME123" };
        var content = new StringContent(JsonConvert.SerializeObject(requestObj), Encoding.UTF8, "application/json");

        // ACT
        var response = await client.PostAsync("/api/game/submit-votes", content);

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task CalculateFinalScores_NoLobby_ShouldReturnNotFound()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new User { Email = "user@test.com", Name = "TestUser" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var content = new StringContent(JsonConvert.SerializeObject("UNKNOWN"), Encoding.UTF8, "application/json");

        // ACT
        var response = await client.PostAsync("/api/game/calculate-final-scores", content);

        // ASSERT
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
