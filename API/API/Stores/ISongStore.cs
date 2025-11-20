using API.Models;
using System.Collections.Generic;

namespace API.Stores
{
    public interface ISongStore
    {
        List<Song> Songs { get; }
        void Reload(string? explicitPath = null);
    }
}