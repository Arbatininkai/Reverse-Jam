using Integrations.Data.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Services.GameService.Models;
using Services.Hubs;
using Services.Models;
using Services.Stores;
using System.Collections.Concurrent;
public class GameService : IGameService
{
    private readonly AppDbContext _dbContext;
    private readonly ILobbyStore _lobbyStore;
    private readonly IHubContext<LobbyHub> _hubContext;

    public GameService(
        AppDbContext dbContext,
        ILobbyStore lobbyStore,
        IHubContext<LobbyHub> hubContext)
    {
        _dbContext = dbContext;
        _lobbyStore = lobbyStore;
        _hubContext = hubContext;
    }

    public async Task SubmitVotesAsync(EndRoundRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.LobbyCode))
            throw new ArgumentException("LobbyCode is required.");

        var lobby = await _dbContext.Lobbies.FirstOrDefaultAsync(l => l.LobbyCode == request.LobbyCode);
        if (lobby is null)
            throw new KeyNotFoundException("Lobby not found.");

        var lobbyScores = _lobbyStore.GetOrCreateLobbyScores(request.LobbyCode);
        lobbyScores.AddVotes(request.Votes, request.Round);
    }

    public async Task<FinalScoreResponse> CalculateFinalScoresAsync(string lobbyCode, int userId)
    {
        var lobby = await _dbContext.Lobbies.FirstOrDefaultAsync(l => l.LobbyCode == lobbyCode);
        if (lobby is null)
            throw new KeyNotFoundException("Lobby not found.");

        var lobbyScores = _lobbyStore.GetOrCreateLobbyScores(lobbyCode);
        var playerScores = lobbyScores.GetPlayerScores();

        if (!playerScores.Any())
            throw new ArgumentException("No scores available.");

        var users = await _dbContext.Users
            .Where(u => playerScores.Select(ps => ps.UserId).Contains(u.Id))
            .ToListAsync();

        var finalScores = playerScores.Select(ps =>
        {
            var user = users.FirstOrDefault(u => u.Id == ps.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {ps.UserId} not found in DB");

            var mapped = new PlayerScore<UserEntity>
            {
                Player = user,
                UserId = ps.UserId,
                PlayerName = ps.PlayerName
            };

            foreach (var kvp in ps.RoundScores)
            {
                mapped.AddScore(
                    kvp.Key,
                    kvp.Value.Score
                );
            }

            return mapped;
        }).ToList();

        var winner = finalScores
            .OrderByDescending(ps => ps.TotalScore)
            .First()
            .Player!;

        if (winner.Id == userId)
        {
            winner.TotalWins++;
            await _dbContext.SaveChangesAsync();
        }

        await _hubContext.Clients
            .Group(lobbyCode)
            .SendAsync("PlayerWon", winner);

        return new FinalScoreResponse
        {
            Scores = finalScores,
            Winner = winner
        };
    }

}
