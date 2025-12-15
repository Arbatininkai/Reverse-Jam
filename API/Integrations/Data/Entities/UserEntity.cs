namespace Integrations.Data.Entities;
using System.Text.Json.Serialization;

public class UserEntity
{
    public int Id { get; set; }

    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Emoji { get; set; }
    public int TotalWins { get; set; } = 0;
    [JsonIgnore]
    public ICollection<LobbyEntity> Lobbies { get; set; } = new List<LobbyEntity>();
    [JsonIgnore]
    public ICollection<RecordingEntity> Recordings { get; set; } = new List<RecordingEntity>();
}
