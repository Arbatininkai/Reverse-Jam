using API.Models;
using System.Collections.Concurrent;

namespace API.Stores
{
    public  class LobbyStore : ILobbyStore
    {

        public  List<Lobby> Lobbies { get; } = new List<Lobby>();
        public Dictionary<string, Dictionary<int, List<VoteDto>>> Votes { get; }


        //not sure if this line need to be here
        public static ConcurrentDictionary<int, Lobby> Lobbies { get; set; } = new ConcurrentDictionary<int, Lobby>();

      
            = new Dictionary<string, Dictionary<int, List<VoteDto>>>();

      
        public List<LobbyScores> AllLobbyScores { get; } = new List<LobbyScores>();

        
        public LobbyScores? GetLobbyScores(string lobbyCode)
        {
            return AllLobbyScores.FirstOrDefault(ls => ls.LobbyCode == lobbyCode);
        }

        
        public  LobbyScores GetOrCreateLobbyScores(string lobbyCode)
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