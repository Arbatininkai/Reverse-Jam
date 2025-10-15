namespace API.Models
{
    public class Song
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;    // Azure blob URL
        public string? Artist { get; set; } 
    }
}
