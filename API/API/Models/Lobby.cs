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
        
        //public string Token { get; set;}

        public static int MaxPlayers { get; set; } = 4;
        public List<User> Players { get; set; } = new List<User>();
        
        public String CreatorId { get; set; }
        public List<String> PlayersIds { get; set; } = new List<String>();
    }
    
}
