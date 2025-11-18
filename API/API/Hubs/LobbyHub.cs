using API.Extensions;
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Hubs
{
    public class LobbyHub : Hub
    {
        private readonly AppDbContext _context;
        public LobbyHub(AppDbContext context) { _context = context; }
        
        // Called when a user joins a lobby (by code or auto-match)
        public async Task JoinLobby(string? lobbyCode)
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdStr, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user");
                return;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return;
            }

            if (!string.IsNullOrEmpty(lobbyCode))
            {
                var lobby = await _context.Lobbies
                    .Include(l => l.Players)
                    .FirstOrDefaultAsync(l => l.LobbyCode == lobbyCode);


                if (lobby == null)
                {
                    await Clients.Caller.SendAsync("Error", "Lobby not found");
                    return;
                }

                LobbyStore.Lobbies.TryAdd(lobby.Id, lobby);
                

                if (lobby.IsFull()) //extension method to check if lobby is full
                {
                    await Clients.Caller.SendAsync("Error", "Lobby is full");
                    return;
                }

                if (lobby.HasGameStarted)
                {
                    await Clients.Caller.SendAsync("Error", "Lobby game has started. No more ability to join");
                    return;
                }

                if (!lobby.Players.Any(p => p.Id == user.Id))
                {
                    lobby.Players.Add(user); // tracked instance
                    await _context.SaveChangesAsync();
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, lobby.LobbyCode);
                await Clients.Group(lobby.LobbyCode).SendAsync("PlayerJoined", user);
                await Clients.Caller.SendAsync("JoinedLobby", lobby);
                return;
            }

            // If no lobby code was provided, auto-match to best available or create new
            var availableLobbies = _context.Lobbies
                .Include(l => l.Players)
                .Where(l => !l.Private && l.Players.Count < l.MaxPlayers && l.HasGameStarted != true)
                .ToList();

            if (!availableLobbies.Any())
            {
                var newLobby = new Lobby
                {
                    Private = false,
                    MaxPlayers = 4,
                    OwnerId = user.Id
                };
                newLobby.Players.Add(user);

                _context.Lobbies.Add(newLobby);
                await _context.SaveChangesAsync();
                LobbyStore.Lobbies.TryAdd(newLobby.Id, newLobby);

                await Groups.AddToGroupAsync(Context.ConnectionId, newLobby.LobbyCode);
                await Clients.Caller.SendAsync("JoinedLobby", newLobby);
                return;
            }

            int maxPlayersNow = availableLobbies.Max(l => l.Players.Count);
            var bestLobbies = availableLobbies
                .Where(l => l.Players.Count == maxPlayersNow)
                .ToList();

            var random = new Random();
            var chosenLobby = bestLobbies[random.Next(bestLobbies.Count)];

            if (!chosenLobby.Players.Any(p => p.Id == user.Id))
            {
                chosenLobby.Players.Add(user);
                await _context.SaveChangesAsync();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, chosenLobby.LobbyCode);
            await Clients.Group(chosenLobby.LobbyCode).SendAsync("PlayerJoined", user);
            await Clients.Caller.SendAsync("JoinedLobby", chosenLobby);
        }

        public async Task StartGame(int lobbyId)
        {
            var userId = int.Parse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User is not found");
                return;
            }

            var lobby = await _context.Lobbies
                    .Include(l => l.Players)
                    .FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby == null)
            { 
                await Clients.Caller.SendAsync("Error", "No lobby found");
                return;
                //LobbyStore.Lobbies.TryAdd(lobby.Id, lobby);
            }

            if (!lobby.IsOwner(user.Id))
            {
                await Clients.Caller.SendAsync("Error", "User is not the owner");
                return;
            }

            // Make the server select a random song for all players to repeat
            if (SongStore.Songs == null || SongStore.Songs.Count == 0)
            {
                await Clients.Caller.SendAsync("Error", "No songs found");
                return;
            }
            lobby.HasGameStarted = true;

            List<Song> songs = new List<Song>();
            var random = new Random();
            for (var i = 0; i < lobby.TotalRounds; i++)
            {
                var song = SongStore.Songs[random.Next(SongStore.Songs.Count)];
                songs.Add(song);
            }
            await _context.SaveChangesAsync();
            await Clients.Group(lobby.LobbyCode).SendAsync("GameStarted", lobby.Id, songs);
        }

        public async Task NextPlayer(int lobbyId)
        {
            var lobby = await _context.Lobbies
                    .Include(l => l.Players)
                    .FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby == null) return;

            // Only owner can advance
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (lobby.OwnerId.ToString() != userId) return;

            if (lobby.CurrentPlayerIndex < lobby.Players.Count - 1)
            {
                lobby.CurrentPlayerIndex++;
            }
            else
            {
                if (lobby.CurrentRound < lobby.TotalRounds - 1)
                {
                    lobby.CurrentRound++;
                    lobby.CurrentPlayerIndex = 0;
                }
            }
            _context.Lobbies.Update(lobby);
            await _context.SaveChangesAsync();
            await Clients.Group(lobby.LobbyCode).SendAsync("LobbyUpdated", lobby);
        }


        public async Task LeaveLobby(int lobbyId)
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                await Clients.Caller.SendAsync("Error", "Invalid user");
                return;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User is not found");
                return;
            }

            var lobby = await _context.Lobbies
                    .Include(l => l.Players)
                    .Include(l => l.Recordings)
                    .FirstOrDefaultAsync(l => l.Id == lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("Error", "No lobby found");
                return;
            }
            var wasOwner = lobby.IsOwner(user.Id);

            var playerToRemove = lobby.Players.FirstOrDefault(p => p.Id == user.Id);
            if (playerToRemove != null)
            {
                lobby.Players.Remove(playerToRemove);
                _context.Entry(playerToRemove).State = EntityState.Detached;
            }
                

            // If owner left, assign a new one
            if (wasOwner && lobby.Players.Any())
            {
                lobby.OwnerId = lobby.Players.First().Id;
            }
            _context.Lobbies.Update(lobby);
            await _context.SaveChangesAsync();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobby.LobbyCode);
            await Clients.Group(lobby.LobbyCode).SendAsync("PlayerLeft", user, lobby.OwnerId);
        }

        public async Task UpdateLobbyWithScores(int lobbyId, object updatedLobby)
        {
            var lobby = LobbyStore.Lobbies.Values.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null) return;

           
            await Clients.Group(lobby.LobbyCode).SendAsync("LobbyUpdated", updatedLobby);

            Console.WriteLine($"Lobby updated with final scores for {lobby.LobbyCode}");
        }
        public async Task NotifyFinalScores(int lobbyId, object scores)
        {
            var lobby = LobbyStore.Lobbies.Values.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null) return;

            
            await Clients.Group(lobby.LobbyCode).SendAsync("FinalScoresReady", scores);
        }
        public async Task NotifyPlayerVoted(int lobbyId)
        {
            var lobby = LobbyStore.Lobbies.Values.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null) return;

            
            await Clients.Group(lobby.LobbyCode).SendAsync("PlayerVoted");

           
        }
    }
}
