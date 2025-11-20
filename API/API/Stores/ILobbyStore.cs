using API.Models;
using System.Collections.Generic;

namespace API.Stores
{
    public interface ILobbyStore
    {
        List<Lobby> Lobbies { get; }
        Dictionary<string, Dictionary<int, List<VoteDto>>> Votes { get; }
        List<LobbyScores> AllLobbyScores { get; }

        LobbyScores? GetLobbyScores(string lobbyCode);
        LobbyScores GetOrCreateLobbyScores(string lobbyCode);
    }
}