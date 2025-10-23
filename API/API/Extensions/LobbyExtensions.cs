using API.Models;

namespace API.Extensions
{
    public static class LobbyExtensions
    {
        public static bool IsFull(this Lobby lobby)
        {
            return lobby.Players.Count >= lobby.MaxPlayers;
        }

        public static bool AddPlayer(this Lobby lobby, User user)
        {
            if (lobby.IsFull() || lobby.Players.Any(p => p.Email == user.Email))
                return false;

            lobby.Players.Add(user);
            return true;
        }

        public static bool RemovePlayer(this Lobby lobby, string email)
        {
            var player = lobby.Players.FirstOrDefault(p => p.Email == email);
            if (player == null) return false;

            lobby.Players.Remove(player);
            return true;
        }

        public static bool IsOwner(this Lobby lobby, int userId)
        {
            return lobby.OwnerId == userId;
        }
    }
}