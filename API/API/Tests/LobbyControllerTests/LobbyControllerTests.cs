using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using API.Models;

[Collection(nameof(DatabaseTestCollection))] 
public class LobbyControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;

    public LobbyControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

 
    private string GenerateFakeJwt(int id, string email)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("tavo_labai_slaptas_raktas_turi_buti_ilgesnis_32_bytes!");

        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
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

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    [Fact]
    public async Task CreateLobby_ValidRequest_ReturnsLobby()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new User { Email = "creator@test.com", Name = "Creator" };
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var requestObj = new
        {
            Private = true,
            AiRate = false,
            HumanRate = true,
            TotalRounds = 3
        };
        var content = new StringContent(JsonConvert.SerializeObject(requestObj), Encoding.UTF8, "application/json");

        // ACT
        var response = await client.PostAsync("/api/lobby/create", content);

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"totalRounds\":3", responseBody);
        Assert.Contains(user.Email, responseBody);
    }
    
    [Fact]
    public async Task LobbyExists_ReturnsOk_WhenLobbyExists()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new User { Email = "player@test.com", Name = "Player" };
        await db.Users.AddAsync(user);

        var lobby = new Lobby { LobbyCode = "ABC123", OwnerId = user.Id };
        lobby.Players.Add(user);
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/api/lobby/exists/ABC123");

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LobbyExists_ReturnsNotFound_WhenLobbyDoesNotExist()
    {
        // ARRANGE
        var client = _factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/api/lobby/exists/WRONG_CODE");

        // ASSERT
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteLobby_ValidRequest_DeletesLobby()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new User { Email = "owner@test.com", Name = "Owner" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var lobby = new Lobby { LobbyCode = "DEL123", OwnerId = user.Id };
        lobby.Players.Add(user);
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        var content = new StringContent(JsonConvert.SerializeObject(lobby.Id), Encoding.UTF8, "application/json");

        // ACT
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/lobby/delete")
        {
            Content = content
        };
        var response = await client.SendAsync(request);

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        db.ChangeTracker.Clear();
        var lobbyExistsInDb = await db.Lobbies.FirstOrDefaultAsync(l => l.Id == lobby.Id);
        Assert.Null(lobbyExistsInDb);
    }
}
