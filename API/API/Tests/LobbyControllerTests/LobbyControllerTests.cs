using API.Models;
using Integrations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Services.LobbyService;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

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
        var user = new UserEntity { Email = "creator@test.com", Name = "Creator" };
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
        var user = new UserEntity { Email = "player@test.com", Name = "Player" };
        await db.Users.AddAsync(user);

        var lobby = new LobbyEntity { LobbyCode = "ABC123", OwnerId = user.Id };
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
        var user = new UserEntity { Email = "owner@test.com", Name = "Owner" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity { LobbyCode = "DEL123", OwnerId = user.Id };
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

    [Fact]
    public async Task GetUserLobbies_ValidRequest_ReturnsUserLobbies()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();

        var user = new UserEntity { Email = "user@test.com", Name = "User" };
        var otherUser = new UserEntity { Email = "other@test.com", Name = "Other" };

        db.Users.AddRange(user, otherUser);
        await db.SaveChangesAsync();

        var lobby1 = new LobbyEntity
        {
            LobbyCode = "ABC123",
            OwnerId = user.Id
        };
        lobby1.Players.Add(user);

        var lobby2 = new LobbyEntity
        {
            LobbyCode = "XYZ999",
            OwnerId = otherUser.Id
        };
        lobby2.Players.Add(otherUser);

        db.Lobbies.AddRange(lobby1, lobby2);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        // ACT
        var response = await client.GetAsync("/api/lobby/user?page=1&pageSize=3");

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<List<LobbyWithScoresDto>>(content);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("ABC123", result.First().Lobby.LobbyCode);
    }

    [Fact]
    public async Task GetUserLobbies_WithRecording_ReturnsLobbyWithRecording()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();

        var user = new UserEntity
        {
            Email = "user@test.com",
            Name = "User"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "REC123",
            OwnerId = user.Id,
            TotalRounds = 3,
            CurrentRound = 1
        };

        lobby.Players.Add(user);

        var recording = new RecordingEntity
        {
            Url = "https://test.com/recording.mp3",
            FileName = "recording.mp3",
            UserId = user.Id,
            Round = 1,
            AiScore = 2.14
        };

        lobby.Recordings.Add(recording);

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);

        // ACT
        var response = await client.GetAsync("/api/lobby/user?page=1&pageSize=3");

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<List<LobbyWithScoresDto>>(content);

        Assert.NotNull(result);
        Assert.Single(result);

        var returnedLobby = result.First().Lobby;
        Assert.Equal("REC123", returnedLobby.LobbyCode);
        Assert.NotEmpty(returnedLobby.Recordings);

        var returnedRecording = returnedLobby.Recordings.First();
        Assert.Equal("recording.mp3", returnedRecording.FileName);
        Assert.Equal(user.Id, returnedRecording.UserId);
        Assert.Equal(1, returnedRecording.Round);
        Assert.Equal(2.14, returnedRecording.AiScore);
    }


}
