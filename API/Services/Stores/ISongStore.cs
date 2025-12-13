using Services.Models;

namespace Services.Stores
{
    public interface ISongStore
    {
        List<Song> Songs { get; }
        void Reload(string? explicitPath = null);
    }
}
