using Microsoft.AspNetCore.Http;

namespace Services.Models
{
    public class RecordingUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string? OriginalSongLyrics { get; set; }
    }
}
