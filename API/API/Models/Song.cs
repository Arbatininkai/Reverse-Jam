namespace API.Models
{
    public struct Song : IEquatable<Song> //struct usage second task, implement IEquatable for tenth task
    {
        public string Name { get; set; }
        public string Url { get; set; } // Azure blob URL
        public string? Artist { get; set; }
        public string? CoverUrl { get; set; }
        public bool Equals(Song other) //IEquatable usage to compare songs by URL
        {
            return string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode()
            => Url?.GetHashCode() ?? 0;
    }
}
