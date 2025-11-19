using System.Text.Json.Serialization;

namespace API.Models
{
    public class Recording
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = new User();
        public int LobbyId { get; set; }
        [JsonIgnore]
        public Lobby Lobby { get; set; } = new Lobby();
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int Round { get; set; }


        public double? Score { get; set; } = null;
        public string? StatusMessage { get; set; } = null;
    }
}
