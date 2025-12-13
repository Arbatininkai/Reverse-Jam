namespace Services.Models
{
    public class RecordingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int LobbyId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public int Round { get; set; }
        public double? AiScore { get; set; }
        public string? StatusMessage { get; set; }
    }
}
