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
        
        public string Token { get; set;}

        public List<String> Players { get; set; } = new List<String>();
    }
}
