using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Integrations.Data.Entities;

public class LobbyEntity
{
    public int Id { get; set; }

    public string LobbyCode { get; set; } = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    public bool Private { get; set; } = false;
    public bool AiRate { get; set; } = true;
    public bool HumanRate { get; set; } = true;

    public int MaxPlayers { get; set; } = 4;
    public int TotalRounds { get; set; } = 1;
    public int CurrentRound { get; set; } = 0;
    public int CurrentPlayerIndex { get; set; } = 0;
    public bool HasGameStarted { get; set; } = false;

    public int OwnerId { get; set; }
    public List<UserEntity> Players { get; set; } = new List<UserEntity>();
    [NotMapped]
    public ICollection<RecordingEntity> Recordings { get; set; } = new List<RecordingEntity>();
}
