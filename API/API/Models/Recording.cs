using System;

namespace API.Models
{
    public class Recording
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

       
        public double? Score { get; set; } = null;          
        public string? StatusMessage { get; set; } = null;  
    }
}