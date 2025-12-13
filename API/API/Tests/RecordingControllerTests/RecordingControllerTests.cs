using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Xunit;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Integrations.Data.Entities;

[Collection(nameof(DatabaseTestCollection))]
public class RecordingsControllerTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;

    public RecordingsControllerTests(IntegrationTestFactory factory)
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
    public async Task UploadRecording_NoFile_ShouldReturnBadRequest()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new UserEntity { Email = "testuser@test.com", Name = "TestUser" };
        db.Users.Add(user);
        db.Lobbies.Add(new LobbyEntity { LobbyCode = "TEST12", OwnerId = user.Id });
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(user.Id, user.Email ?? "");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var form = new MultipartFormDataContent();

        // ACT
        var response = await client.PostAsync("/api/recordings/upload/1/0", form);

        // ASSERT
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task UploadRecording_ShouldSaveFileAndReturnOk()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var user = new UserEntity { Email = "uploader@test.com", Name = "Uploader" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var savedUser = await db.Users.SingleAsync(u => u.Email == "uploader@test.com");
        var lobby = new LobbyEntity { LobbyCode = "AUD123", Players = new List<UserEntity> { user } };
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var jwt = GenerateFakeJwt(savedUser.Id, savedUser.Email ?? "");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        
        
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake audio"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        form.Add(fileContent, "File", "test.mp3");

        // ACT
        var response = await client.PostAsync($"/api/recordings/upload/{lobby.Id}/0", form);

        // ASSERT
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("url", await response.Content.ReadAsStringAsync());
    }


    [Fact]
    public async Task GetRecording_ShouldReturnNotFound_WhenFileDoesNotExist()
    {
        // ARRANGE
        var client = _factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/api/recordings/WRONG_LOBBY/fakefile.mp3");

        // ASSERT
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task GetAllRecordings_PrivateLobbyWithoutAuth_ShouldReturnUnauthorized()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        db.Lobbies.Add(new LobbyEntity { LobbyCode = "PRI123", Private = true });
        await db.SaveChangesAsync();

        var client = _factory.CreateClient();

        // ACT
        var response = await client.GetAsync("/api/recordings/PRI123/recordings");

        // ASSERT
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
