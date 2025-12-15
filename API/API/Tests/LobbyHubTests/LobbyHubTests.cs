using Integrations.Data.Entities;
using Services.Hubs;
using Services.Models;
using Services.Stores;
using Services.SongService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

[Collection(nameof(DatabaseTestCollection))]
public class LobbyHubTests : IAsyncLifetime
{
    private readonly IntegrationTestFactory _factory;

    public LobbyHubTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private HubCallerContext GetMockedContext(int userId)
    {
        var mockContext = new Mock<HubCallerContext>();
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));
        mockContext.Setup(m => m.User).Returns(claimsPrincipal);
        mockContext.Setup(m => m.ConnectionId).Returns(Guid.NewGuid().ToString());
        return mockContext.Object;
    }

    private IHubCallerClients GetMockedClients(out Mock<ISingleClientProxy> callerProxy, out Mock<IClientProxy> groupProxy)
    {
        callerProxy = new Mock<ISingleClientProxy>();
        groupProxy = new Mock<IClientProxy>();
        var clientsMock = new Mock<IHubCallerClients>();
        clientsMock.Setup(c => c.Caller).Returns(callerProxy.Object);
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxy.Object);
        return clientsMock.Object;
    }

    [Fact]
    public async Task JoinLobby_WithoutProdivdingCode_ShouldCreateLobby()
    {
        // ARRANGE
        var db = _factory.CreateDbContext();
        var mockGroups = new Mock<IGroupManager>();
        var mockSongService = new Mock<ISongService>();

        var user = new UserEntity { Name = "TestUser", Email = "test@gmail.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object, mockGroups.Object)
        {
            Context = GetMockedContext(user.Id),
            Clients = GetMockedClients(out var caller, out var group)
        };

        // ACT
        await hub.JoinLobby(null);

        // ASSERT
        var lobby = await db.Lobbies
            .Include(l => l.Players)
            .FirstOrDefaultAsync();

        Assert.NotNull(lobby);
        Assert.Single(lobby!.Players);
        Assert.Equal(user.Id, lobby.Players.First().Id);

        caller.Verify(c => c.SendCoreAsync(
            "JoinedLobby",
            It.Is<object[]>(args => args.Length == 1 && args[0] is LobbyEntity),
            It.IsAny<CancellationToken>()), Times.Once);

        mockGroups.Verify(g => g.AddToGroupAsync(
            It.IsAny<string>(),
            lobby.LobbyCode,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinLobby_CodeProvided_ShouldAddTheUser()
    {
        // ARRANGE
        var mockGroups = new Mock<IGroupManager>();
        var mockSongService = new Mock<ISongService>();
        var db = _factory.CreateDbContext();

        var user1 = new UserEntity { Name = "User1", Email = "user1@test.com" };
        var user2 = new UserEntity { Name = "User2", Email = "user2@test.com" };
        var user3 = new UserEntity { Name = "User3", Email = "user3@test.com" };

        db.Users.AddRange(user1, user2, user3);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "ABC123",
            OwnerId = user1.Id,
            Players = new List<UserEntity> { user1, user2 }
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object, mockGroups.Object)
        {
            Context = GetMockedContext(user3.Id),
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        string lobbyCode = "ABC123";

        //ACT
        await hub.JoinLobby(lobbyCode);

        //ASSERT
        var updatedLobby = await db.Lobbies
            .Include(l => l.Players)
            .FirstAsync(l => l.Id == lobby.Id);

        Assert.Contains(updatedLobby.Players, p => p.Id == user3.Id);

        callerMock.Verify(c => c.SendCoreAsync(
            "JoinedLobby",
            It.Is<object[]>(args => args.Length == 1 && args[0] is LobbyEntity),
            It.IsAny<CancellationToken>()), Times.Once);

        mockGroups.Verify(g => g.AddToGroupAsync(
            It.IsAny<string>(),
            lobby.LobbyCode,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("AB52FC")]
    [InlineData("AAAAAA")]
    public async Task JoinLobby_WrongCodeProvided_ShouldReturnError(string invalidLobbyCode)
    {
        // ARRANGE
        var mockGroups = new Mock<IGroupManager>();
        var mockSongService = new Mock<ISongService>();
        var db = _factory.CreateDbContext();

        var user1 = new UserEntity { Name = "User1", Email = "user1@test.com" };
        var user2 = new UserEntity { Name = "User2", Email = "user2@test.com" };
        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "ABC123",
            OwnerId = user1.Id,
            Players = new List<UserEntity> { user1 }
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object, mockGroups.Object)
        {
            Context = GetMockedContext(user2.Id),
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        // ACT
        await hub.JoinLobby(invalidLobbyCode);

        // ASSERT
        var anyLobby = await db.Lobbies
            .Include(l => l.Players)
            .FirstOrDefaultAsync();

        if (anyLobby != null)
        {
            Assert.DoesNotContain(anyLobby.Players, p => p.Id == user2.Id);
        }

        // Verify that the caller received an error
        callerMock.Verify(c => c.SendCoreAsync(
            "Error",
            It.Is<object[]>(args => args.Length == 1 && args[0].ToString()!.Contains("Lobby not found")),
            It.IsAny<CancellationToken>()), Times.Once);

        mockGroups.Verify(g => g.AddToGroupAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LeaveLobby_PlayerLeaving_ShouldRemovePlayerAndAssignNewOwner()
    {
        // ARRANGE
        var mockGroups = new Mock<IGroupManager>();
        var mockSongService = new Mock<ISongService>();
        var db = _factory.CreateDbContext();

        var user1 = new UserEntity { Name = "User1", Email = "user1@test.com" };
        var user2 = new UserEntity { Name = "User2", Email = "user2@test.com" };

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "ABC123",
            OwnerId = user1.Id,
            Players = new List<UserEntity> { user1, user2 }
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object, mockGroups.Object)
        {
            Context = GetMockedContext(user1.Id),
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        // ACT
        await hub.LeaveLobby(lobby.Id);

        // ASSERT
        var updatedLobby = await db.Lobbies
            .Include(l => l.Players)
            .FirstAsync(l => l.Id == lobby.Id);

        Assert.Single(updatedLobby.Players);
        Assert.Contains(updatedLobby.Players, p => p.Id == user2.Id);
        Assert.DoesNotContain(updatedLobby.Players, p => p.Id == user1.Id);
        Assert.Equal(user2.Id, updatedLobby.OwnerId);

        // ASSERT
        groupMock.Verify(g => g.SendCoreAsync(
            "PlayerLeft",
             It.Is<object[]>(args =>
                args.Length == 3 &&
                args[0] != null && ((UserEntity)args[0]).Id == user1.Id &&
                args[1] != null && ((int)args[1] == user2.Id || (int)args[1] == lobby.OwnerId) &&
                args[2] != null && ((LobbyDto)args[2]).Id == lobby.Id
            ),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // ASSERT - No error sent to the caller
        callerMock.Verify(c => c.SendCoreAsync(
            It.Is<string>(m => m == "Error"),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        // ASSERT - Player removed from SignalR group
        mockGroups.Verify(g => g.RemoveFromGroupAsync(
            It.IsAny<string>(),
            lobby.LobbyCode,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartGame_UserNotOwner_ShouldSendError()
    {
        var db = _factory.CreateDbContext();
        var mockSongService = new Mock<ISongService>();

        var owner = new UserEntity { Name = "Owner", Email = "owner@test.com" };
        var otherUser = new UserEntity { Name = "Other", Email = "other@test.com" };
        db.Users.AddRange(owner, otherUser);

        var lobby = new LobbyEntity
        {
            LobbyCode = "ABC123",
            OwnerId = owner.Id,
            Players = new List<UserEntity> { owner, otherUser },
            TotalRounds = 1
        };
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object)
        {
            Context = GetMockedContext(otherUser.Id),
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        await hub.StartGame(lobby.Id);

        callerMock.Verify(c => c.SendCoreAsync(
            "Error",
            It.Is<object[]>(args => args.Length == 1 && args[0].ToString()!.Contains("User is not the owner")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartGame_Valid_ShouldSetHasGameStartedAndNotifyGroup()
    {
        var db = _factory.CreateDbContext();
        var mockSongService = new Mock<ISongService>();

        var owner = new UserEntity { Name = "Owner", Email = "owner@test.com" };
        var otherUser = new UserEntity { Name = "Other", Email = "other@test.com" };
        db.Users.AddRange(owner, otherUser);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity { LobbyCode = "ABC123", OwnerId = owner.Id, TotalRounds = 2, Players = new List<UserEntity> { owner, otherUser } };
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        mockSongService.Setup(x => x.GetAllSongs()).Returns(new List<Song> { new Song { Name = "Test", Url = "test" } });

        var hub = new LobbyHub(db, mockSongService.Object)
        {
            Context = GetMockedContext(owner.Id),
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        await hub.StartGame(lobby.Id);

        var updatedLobby = await db.Lobbies.FirstAsync(l => l.Id == lobby.Id);
        Assert.True(updatedLobby.HasGameStarted);

        groupMock.Verify(g => g.SendCoreAsync(
            "GameStarted",
            It.Is<object[]>(args => args.Length == 2 && (int)args[0] == lobby.Id && args[1] is List<Song>),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NextPlayer_WhenOwnerPressesNextPlayerTwice_ShouldChangeToNextRoundAndResetPlayerIndex()
    {
        //ARRANGE
        var mockGroups = new Mock<IGroupManager>();
        var mockSongService = new Mock<ISongService>();
        var db = _factory.CreateDbContext();

        var user1 = new UserEntity { Name = "User1", Email = "user1@test.com" };
        var user2 = new UserEntity { Name = "User2", Email = "user2@test.com" };

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "ABC123",
            OwnerId = user1.Id,
            TotalRounds = 2,
            Players = new List<UserEntity> { user1, user2 }
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object, mockGroups.Object)
        {
            Context = GetMockedContext(user1.Id),
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        //ACT
        await hub.NextPlayer(lobby.Id);
        await hub.NextPlayer(lobby.Id);

        // ASSERT
        var updatedLobby = await db.Lobbies
            .Include(l => l.Players)
            .FirstAsync(l => l.Id == lobby.Id);

        Assert.True(updatedLobby.CurrentPlayerIndex == 0);
        Assert.True(updatedLobby.CurrentRound == 1);

        groupMock.Verify(g => g.SendCoreAsync(
            "LobbyUpdated",
            It.Is<object[]>(args => args.Length == 1 &&
                    args[0] != null &&
                    ((LobbyEntity)args[0]).Id == updatedLobby.Id &&
                    ((LobbyEntity)args[0]).CurrentPlayerIndex == updatedLobby.CurrentPlayerIndex &&
                    ((LobbyEntity)args[0]).CurrentRound == updatedLobby.CurrentRound),
            It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task NotifyFinalScores_FinalScoresSubmitted_ShouldSendScoresToGroup()
    {
        // ARRANGE
        var mockGroups = new Mock<IGroupManager>();
        var mockSongService = new Mock<ISongService>();
        var db = _factory.CreateDbContext();

        var user1 = new UserEntity { Name = "User1", Email = "user1@test.com" };
        db.Users.Add(user1);
        await db.SaveChangesAsync();

        var lobby = new LobbyEntity
        {
            LobbyCode = "ABC123",
            Players = new List<UserEntity> { user1 }
        };

        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object, mockGroups.Object)
        {
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        var scores = new { UserId = 1, Score = 5 };

        // ACT
        await hub.NotifyFinalScores(lobby.Id, scores);

        // ASSERT
        groupMock.Verify(g => g.SendCoreAsync(
            "FinalScoresReady",
            It.Is<object[]>(args => args.Length == 1 && args[0] == scores),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPlayerVoted_Voted_ShouldSendToGroup()
    {
        var db = _factory.CreateDbContext();
        var mockSongService = new Mock<ISongService>();

        var lobby = new LobbyEntity {LobbyCode = "ABC123" };
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object)
        {
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        await hub.NotifyPlayerVoted(lobby.Id);

        groupMock.Verify(g => g.SendCoreAsync(
            "PlayerVoted",
            It.Is<object[]>(args => args.Length == 0 || args == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLobbyWithScores_Updated_ShouldSendToGroup()
    {
        var db = _factory.CreateDbContext();
        var mockSongService = new Mock<ISongService>();

        var lobby = new LobbyEntity { LobbyCode = "ABC123" };
        db.Lobbies.Add(lobby);
        await db.SaveChangesAsync();

        var hub = new LobbyHub(db, mockSongService.Object)
        {
            Clients = GetMockedClients(out var callerMock, out var groupMock)
        };

        await hub.UpdateLobbyWithScores(lobby.Id, lobby);

        groupMock.Verify(g => g.SendCoreAsync(
            "LobbyUpdated",
            It.Is<object[]>(args => args.Length == 1 || args[0] == lobby),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}