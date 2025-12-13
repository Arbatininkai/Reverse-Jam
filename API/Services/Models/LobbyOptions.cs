namespace Services.Models
{
    public class LobbyOptions
    {
        public bool Private { get; set; }
        public bool AiRate { get; set; }
        public bool HumanRate { get; set; }
        public int MaxPlayers { get; set; } = 4;
        public int TotalRounds { get; set; } = 3;
    }
}
