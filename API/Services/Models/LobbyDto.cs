using Integrations.Data.Entities;

namespace Services.Models
{
    public class LobbyDto
    {
        public int Id { get; set; }
        public string LobbyCode { get; set; } = string.Empty;
        public bool Private { get; set; }
        public bool AiRate { get; set; }
        public bool HumanRate { get; set; }
        public int MaxPlayers { get; set; }
        public int TotalRounds { get; set; }
        public int OwnerId { get; set; }
        public bool HasGameStarted { get; set; }
        public int CurrentRound { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public List<UserDto> Players { get; set; } = new();  
    }
}
