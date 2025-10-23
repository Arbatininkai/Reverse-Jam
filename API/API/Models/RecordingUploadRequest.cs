using Microsoft.AspNetCore.Http;

namespace API.Models
{
    public class RecordingUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}