namespace API.Models
{
    public struct Song : IEquatable<Song> //struct usage second task, implement IEquatable for tenth task
    {
        private string _name;
        private string _url;
        public string Name
        {
            get => _name;
            init
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Song name cannot be empty.");
                _name = value;
            }
        }
        public string Url
        {
            get => _url;
            init
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Song URL cannot be empty.");
                _url = value;
            }
        }
        public string? Artist { get; set; }
        public string? CoverUrl { get; set; }
        public string? Lyrics { get; set; }
        public bool Equals(Song other) //IEquatable usage to compare songs by URL
        {
            return string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
        }
        public override int GetHashCode()
            => Url?.GetHashCode() ?? 0;
    }
}
