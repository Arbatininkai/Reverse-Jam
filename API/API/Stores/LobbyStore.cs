using API.Models;
using System.Collections.Concurrent;

namespace API.Stores
{
    public static class LobbyStore
    {
        public static ConcurrentDictionary<int, Lobby> Lobbies { get; set; } = new ConcurrentDictionary<int, Lobby>();
        public static Dictionary<string, Dictionary<int, List<VoteDto>>> Votes { get; set; }
            = new Dictionary<string, Dictionary<int, List<VoteDto>>>();


        public static ConcurrentDictionary<string, LobbyScores> AllLobbyScores { get; set; } = new();


        public static LobbyScores? GetLobbyScores(string lobbyCode)
        {
            AllLobbyScores.TryGetValue(lobbyCode, out var scores);
            return scores;
        }

        
        public static LobbyScores GetOrCreateLobbyScores(string lobbyCode)
        {
            var lobbyScores = GetLobbyScores(lobbyCode);
            if (lobbyScores == null)
            {
                lobbyScores = new LobbyScores { LobbyCode = lobbyCode };
                AllLobbyScores.TryAdd(lobbyCode, lobbyScores);
            }
            return lobbyScores;
        }
    }
}