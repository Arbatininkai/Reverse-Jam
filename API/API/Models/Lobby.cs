namespace API.Models
{
    public class Lobby
    {
        public int Id { get; set; }

        public string LobbyCode { get; set; } = string.Empty;

        //defaultines reiksmes, is frontendo bus galima gaut kitokias reiksmes
        public bool Private { get; set; } = false;
        public bool AiRate { get; set; } = true;
        public bool HumanRate { get; set; } = true;

        public int MaxPlayers { get; set; } = 4;
        public int TotalRounds { get; set; } = 1;
        public int CurrentRound { get; set; } = 0;
        public int CurrentPlayerIndex { get; set; } = 0;  // Which player's turn to listen
        public bool HasGameStarted { get; set; } = false;
        public int OwnerId { get; set; }
        public List<User> Players { get; set; } = new List<User>();
        public List<List<Recording>> RecordingsByRound { get; set; } = new List<List<Recording>>();
    }
}
