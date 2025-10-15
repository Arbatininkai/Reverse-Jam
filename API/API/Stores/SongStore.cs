using API.Models;
using System.Collections.Generic;

namespace API.Stores
{
    public static class SongStore
    {
        public static List<Song> Songs { get; set; } = new List<Song>
        {
            new Song { Name = "Crazy", Url = "https://songstorage25.blob.core.windows.net/songs/Gnarls Barkley - Crazy.wav", Artist = "Gnarls Barkley" },
            new Song { Name = "Billie Jean", Url = "https://songstorage25.blob.core.windows.net/songs/Michael Jackson - Billie Jean (Official Video).wav", Artist = "Michael Jackson" }
        };
    }
}