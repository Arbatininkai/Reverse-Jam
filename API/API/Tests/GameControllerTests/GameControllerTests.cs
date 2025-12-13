using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Integrations.Data.Entities;
using API.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Services.GameService.Models;

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
        var user = new UserEntity { Email = "test@test.com", Name = "TestUser" };
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

        var user = new UserEntity { Email = "test@test.com", Name = "TestUser" };
        var lobby = new LobbyEntity { LobbyCode = "GAME123", Players = new List<UserEntity> { user } };
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
        var user = new UserEntity { Email = "user@test.com", Name = "TestUser" };
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

    [Fact]
    public async Task CalculateFinalScores_ProvidedLobby_ShouldReturnFinalCalculatedScores()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();

        var user1 = new UserEntity { Email = "user@test.com", Name = "TestUser" };
        var user2 = new UserEntity { Email = "user2@test.com", Name = "SecondUser" };

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "GGG123",
            Players = new List<UserEntity> { user1, user2 }
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateFakeJwt(user1.Id, user1.Email!));

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateFakeJwt(user2.Id, user2.Email!));

        // Submit votes
        await client.PostAsync(
            "/api/game/submit-votes",
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    LobbyCode = "GGG123",
                    Round = 1,
                    Votes = new[]
                    {
                    new { UserId = user1.Id, Score = 5 }
                    }
                }),
                Encoding.UTF8,
                "application/json"));

        await client2.PostAsync(
            "/api/game/submit-votes",
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    LobbyCode = "GGG123",
                    Round = 1,
                    Votes = new[]
                    {
                    new { UserId = user2.Id, Score = 10 }
                    }
                }),
                Encoding.UTF8,
                "application/json"));

        // ACT
        var response = await client.PostAsync(
            "/api/game/calculate-final-scores",
            new StringContent(JsonConvert.SerializeObject("GGG123"), Encoding.UTF8, "application/json"));

        var response2 = await client2.PostAsync(
            "/api/game/calculate-final-scores",
            new StringContent(JsonConvert.SerializeObject("GGG123"), Encoding.UTF8, "application/json"));


        // ASSERT
        response.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<CalculateFinalScoresResponse>(body)!;

        Assert.Equal(2, result.Scores.Count);

        var score1 = result.Scores.Single(s => s.UserId == user1.Id);
        var score2 = result.Scores.Single(s => s.UserId == user2.Id);

        db.Dispose();

        var dbAfter = _factory.CreateDbContext();

        var winnerFromDb = await dbAfter.Users
            .SingleAsync(u => u.Id == user2.Id);

        Assert.Equal(5, score1.TotalScore);
        Assert.Equal(10, score2.TotalScore);

        Assert.Equal(5, score1.RoundScores[1].Score);
        Assert.Equal(10, score2.RoundScores[1].Score);

        Assert.Equal(user2.Id, result.Winner.Id);
        Assert.Equal(result.Winner.Id, winnerFromDb.Id);
        Assert.Equal(1, winnerFromDb.TotalWins);
    }

}
