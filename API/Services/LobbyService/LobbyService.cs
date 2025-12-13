using Integrations.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Services.Hubs;
using Services.Models;
using Services.Stores;
using Services.Utils;

namespace Services.LobbyService
{
    public class LobbyService : ILobbyService
    {
        private readonly IHubContext<LobbyHub> _hubContext;
        private readonly ILobbyStore _lobbyStore;
        private readonly AppDbContext _dbContext;

        public LobbyService(
            IHubContext<LobbyHub> hubContext,
            ILobbyStore lobbyStore,
            AppDbContext dbContext)
        {
            _hubContext = hubContext;
            _lobbyStore = lobbyStore;
            _dbContext = dbContext;
        }

        public async Task<LobbyDto> CreateLobbyAsync(int creatorId, LobbyOptions options)
        {
            var creator = await _dbContext.Users.FindAsync(creatorId)
                ?? throw new Exception("User does not exist");

            var newLobby = new LobbyEntity
            {
                Private = options.Private,
                AiRate = options.AiRate,
                TotalRounds = options.TotalRounds,
                HumanRate = options.HumanRate,
                OwnerId = creator.Id,
                MaxPlayers = options.MaxPlayers
            };

            newLobby.Players.Add(creator);
            _dbContext.Lobbies.Add(newLobby);
            await _dbContext.SaveChangesAsync();

            return new LobbyDto
            {
                Id = newLobby.Id,
                Players = newLobby.Players.Select(p => new UserDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    Email = p.Email ?? "",
                    PhotoUrl = p.PhotoUrl ?? "",
                    Emoji = p.Emoji ?? ""
                }).ToList(),
                LobbyCode = newLobby.LobbyCode,
                Private = newLobby.Private,
                AiRate = newLobby.AiRate,
                HumanRate = newLobby.HumanRate,
                MaxPlayers = newLobby.MaxPlayers,
                TotalRounds = newLobby.TotalRounds,
                OwnerId = newLobby.OwnerId,
                HasGameStarted = newLobby.HasGameStarted,
                CurrentRound = newLobby.CurrentRound,
                CurrentPlayerIndex = newLobby.CurrentPlayerIndex
            };
        }

        public async Task<bool> LobbyExistsAsync(string code)
        {
            return await _dbContext.Lobbies.AnyAsync(l =>
                l.LobbyCode.ToLower() == code.ToLower());
        }

        public async Task<IEnumerable<LobbyWithScoresDto>> GetPlayerLobbiesAsync(int userId)
        {
            var lobbies = await _dbContext.Lobbies
                .Include(l => l.Players)
                .Where(l => l.Players.Any(p => p.Id == userId))
                .ToListAsync();

            if (!lobbies.Any())
                return new List<LobbyWithScoresDto>();

            var result = lobbies.Select(l => new LobbyWithScoresDto
            {
                Lobby = new LobbyDto
                {
                    Id = l.Id,
                    LobbyCode = l.LobbyCode,
                    Private = l.Private,
                    AiRate = l.AiRate,
                    HumanRate = l.HumanRate,
                    MaxPlayers = l.MaxPlayers,
                    TotalRounds = l.TotalRounds,
                    OwnerId = l.OwnerId,
                    HasGameStarted = l.HasGameStarted,
                    CurrentRound = l.CurrentRound,
                    CurrentPlayerIndex = l.CurrentPlayerIndex
                },
                Scores = _lobbyStore.GetLobbyScores(l.LobbyCode)
            });

            return result;
        }

        public async Task<bool> DeleteLobbyAsync(int lobbyId)
        {
            var lobby = await _dbContext.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyId)
                ?? throw new Exception("Lobby not found");

            _dbContext.Lobbies.Remove(lobby);
            await _dbContext.SaveChangesAsync();

            try
            {
                var recordingsPath = Path.Combine(Directory.GetCurrentDirectory(), "recordings");
                if (Directory.Exists(recordingsPath))
                {
                    Directory.Delete(recordingsPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up recordings : {ex.Message}");
            }

            await _hubContext.Clients.Group(lobby.LobbyCode).SendAsync("LobbyDeleted");

            return true;
        }
    }
}
