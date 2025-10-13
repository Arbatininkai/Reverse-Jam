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

                if (lobby.Players.Count >= lobby.MaxPlayers)
                {
                    await Clients.Caller.SendAsync("Error", "Lobby is full");
                    return;
                }

                if (!lobby.Players.Any(p => p.Id == user.Id))
                    lobby.Players.Add(user);

                await Groups.AddToGroupAsync(Context.ConnectionId, lobby.LobbyCode);
                await Clients.Group(lobby.LobbyCode).SendAsync("PlayerJoined", user);
                await Clients.Caller.SendAsync("JoinedLobby", lobby);
                return;
            }

            // If no lobby code was provided, auto-match to best available or create new
            var availableLobbies = LobbyStore.Lobbies
                .Where(l => !l.Private && l.Players.Count < l.MaxPlayers)
                .ToList();

            if (availableLobbies.Count == 0)
            {
                var newLobby = new Lobby
                {
                    Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                    Private = false,
                    MaxPlayers = 4
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

            if (!chosenLobby.Players.Any(p => p.Id == user.Id))
                chosenLobby.Players.Add(user);

            await Groups.AddToGroupAsync(Context.ConnectionId, chosenLobby.LobbyCode);
            await Clients.Group(chosenLobby.LobbyCode).SendAsync("PlayerJoined", user);
            await Clients.Caller.SendAsync("JoinedLobby", chosenLobby);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = UserStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (user != null)
            {
                // Remove user from all lobbies
                var lobby = LobbyStore.Lobbies.FirstOrDefault(l => l.Players.Any(p => p.Id == user.Id));
                if (lobby != null)
                {
                    lobby.Players.RemoveAll(p => p.Id == user.Id);
                    await Clients.Group(lobby.LobbyCode).SendAsync("PlayerLeft", user);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
