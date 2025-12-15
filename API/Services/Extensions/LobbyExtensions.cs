using Integrations.Data.Entities;

namespace Services.Extensions
{
    public static class LobbyExtensions
    {
        public static bool IsFull(this LobbyEntity lobby)
        {
            return lobby.Players.Count >= lobby.MaxPlayers;
        }

        public static bool IsOwner(this LobbyEntity lobby, int userId)
        {
            return lobby.OwnerId == userId;
        }
    }
}