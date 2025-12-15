using Integrations.Data.Entities;
using Services.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Services.Stores
{
    public class LobbyStore : ILobbyStore
    {
        private static readonly ConcurrentDictionary<string, LobbyScores> _allLobbyScores = new();

        public List<LobbyScores> AllLobbyScores => _allLobbyScores.Values.ToList();

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
