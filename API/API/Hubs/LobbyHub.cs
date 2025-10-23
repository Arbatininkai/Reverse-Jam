using API.Extensions;
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace API.Hubs
{
    public class LobbyHub : Hub
    {
        // Called when a user joins a lobby (by code or auto-match)
        public async Task JoinLobby(string? lobbyCode)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = UserStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return;
            }


            if (!string.IsNullOrEmpty(lobbyCode))
            {
                var lobby = LobbyStore.Lobbies.FirstOrDefault(l =>
                    l.LobbyCode.Equals(lobbyCode, StringComparison.OrdinalIgnoreCase));

                if (lobby == null)
                {
                    await Clients.Caller.SendAsync("Error", "Lobby not found");
                    return;
                }

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

                lobby.AddPlayer(user); //extension method

                await Groups.AddToGroupAsync(Context.ConnectionId, lobby.LobbyCode);
                await Clients.Group(lobby.LobbyCode).SendAsync("PlayerJoined", user);
                await Clients.Caller.SendAsync("JoinedLobby", lobby);
                return;
            }

            // If no lobby code was provided, auto-match to best available or create new
            var availableLobbies = LobbyStore.Lobbies
                .Where(l => !l.Private && l.Players.Count < l.MaxPlayers && l.HasGameStarted != true)
                .ToList();

            if (availableLobbies.Count == 0)
            {
                var newLobby = new Lobby
                {
                    Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                    Private = false,
                    MaxPlayers = 4,
                    OwnerId = user.Id
                };
                newLobby.Players.Add(user);
                LobbyStore.Lobbies.Add(newLobby);

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

            chosenLobby.AddPlayer(user);

            await Groups.AddToGroupAsync(Context.ConnectionId, chosenLobby.LobbyCode);
            await Clients.Group(chosenLobby.LobbyCode).SendAsync("PlayerJoined", user);
            await Clients.Caller.SendAsync("JoinedLobby", chosenLobby);
        }

        public async Task StartGame(int lobbyId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = UserStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User is not found");
                return;
            }

            var lobby = LobbyStore.Lobbies.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("Error", "No lobby found");
                return;
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

            var random = Random.Shared;
            var song = SongStore.Songs[random.Next(SongStore.Songs.Count)];

            await Clients.Group(lobby.LobbyCode).SendAsync("GameStarted", lobby.Id, song);
        }

        public async Task LeaveLobby(int lobbyId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = UserStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User is not found");
                return;
            }

            var lobby = LobbyStore.Lobbies.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null)
            {
                await Clients.Caller.SendAsync("Error", "No lobby found");
                return;
            }

            // If the owner leaves, make the first person the new onwer
            if (!lobby.IsOwner(user.Id))
            {
                lobby.OwnerId = lobby.Players.First().Id;
            }
            lobby.Players.RemoveAll(p => p.Id == user.Id);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobby.LobbyCode);
            await Clients.Group(lobby.LobbyCode).SendAsync("PlayerLeft", user, lobby.OwnerId);
        }

    }
}
