using API.Models;
using System.Collections.Concurrent;

namespace API.Stores
{
    public class LobbyStore : ILobbyStore
    {
        private static readonly ConcurrentDictionary<int, Lobby> _lobbies = new();
        private static readonly Dictionary<string, Dictionary<int, List<VoteDto>>> _votes = new();
        private static readonly ConcurrentDictionary<string, LobbyScores> _allLobbyScores = new();

        public List<Lobby> Lobbies => _lobbies.Values.ToList();
        public Dictionary<string, Dictionary<int, List<VoteDto>>> Votes => _votes;
        public List<LobbyScores> AllLobbyScores => _allLobbyScores.Values.ToList();

        public static ConcurrentDictionary<int, Lobby> LobbiesDict => _lobbies;

        public LobbyScores? GetLobbyScores(string lobbyCode)
        {
            _allLobbyScores.TryGetValue(lobbyCode, out var scores);
            return scores;
        }

        public LobbyScores GetOrCreateLobbyScores(string lobbyCode)
        {
            var lobbyScores = GetLobbyScores(lobbyCode);
            if (lobbyScores == null)
            {
                lobbyScores = new LobbyScores { LobbyCode = lobbyCode };
                _allLobbyScores.TryAdd(lobbyCode, lobbyScores);
            }
            return lobbyScores;
        }
    }
}