using API.Models;
using System.Collections.Concurrent;

namespace API.Stores
{
    public static class LobbyStore
    {
        public static ConcurrentDictionary<int, Lobby> Lobbies { get; set; } = new ConcurrentDictionary<int, Lobby>();
        public static Dictionary<string, Dictionary<int, List<VoteDto>>> Votes { get; set; }
            = new Dictionary<string, Dictionary<int, List<VoteDto>>>();

      
        public static List<LobbyScores> AllLobbyScores { get; set; } = new List<LobbyScores>();

        
        public static LobbyScores? GetLobbyScores(string lobbyCode)
        {
            return AllLobbyScores.FirstOrDefault(ls => ls.LobbyCode == lobbyCode);
        }

        
        public static LobbyScores GetOrCreateLobbyScores(string lobbyCode)
        {
            var lobbyScores = GetLobbyScores(lobbyCode);
            if (lobbyScores == null)
            {
                lobbyScores = new LobbyScores { LobbyCode = lobbyCode };
                AllLobbyScores.Add(lobbyScores);
            }
            return lobbyScores;
        }
    }
}