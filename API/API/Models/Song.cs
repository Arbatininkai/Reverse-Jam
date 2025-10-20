namespace API.Models
{
    public struct Song //struct usage second task
    {
        public string Name { get; set; }
        public string Url { get; set; } // Azure blob URL
        public string? Artist { get; set; }
        public string? CoverUrl { get; set; }
    }
}
