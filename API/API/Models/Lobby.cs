namespace API.Models
{
    public class Lobby
    {
        public int Id { get; set; }
        public int LobbyCode { get; set; } // random 4 simbolių kodas
        
        //defaultines reiksmes, is frontendo bus galima gaut kitokias reiksmes
        public bool Private { get; set; } = false;
        public bool AiRate { get; set; } = true;
        public bool HumanRate { get; set; } = true;

        public int MaxPlayers { get; set; } = 4;
        public bool HasGameStarted { get; set; } = false;
        public int OwnerId { get; set; }
        public List<User> Players { get; set; } = new List<User>();
    }
}
