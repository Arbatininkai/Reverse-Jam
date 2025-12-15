using Integrations.Data.Entities;
using Services.Models;

namespace Services.Stores
{
    public interface ILobbyStore
    {
        List<LobbyScores> AllLobbyScores { get; }

        LobbyScores? GetLobbyScores(string lobbyCode);
        LobbyScores GetOrCreateLobbyScores(string lobbyCode);
    }
}