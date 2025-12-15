using Newtonsoft.Json;

namespace Integrations.Data.Entities;

public class RecordingEntity
{
    public int Id { get; set; }

    public int UserId { get; set; }
    [JsonIgnore]
    public UserEntity User { get; set; } = null!;

    public int LobbyId { get; set; }
    [JsonIgnore]
    public LobbyEntity Lobby { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int Round { get; set; }

    public double? AiScore { get; set; } = 0;
    public string? StatusMessage { get; set; }
}
