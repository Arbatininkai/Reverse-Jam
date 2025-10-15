using API.Models;
using System.Collections.Generic;

namespace API.Stores
{
    public static class SongStore
    {
        public static List<Song> Songs { get; set; } = new List<Song>
        {
            new Song { Name = "Crazy", Url = "https://songstorage25.blob.core.windows.net/songs/Gnarls Barkley - Crazy.mp3", Artist = "Gnarls Barkley", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Crazy.jpg" },
            new Song { Name = "Billie Jean", Url = "https://songstorage25.blob.core.windows.net/songs/Michael Jackson - Billie Jean.mp3", Artist = "Michael Jackson", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Billie jean.jpg" },
            new Song { Name = "Without me", Url = "https://songstorage25.blob.core.windows.net/songs/Eminem - Without Me.mp3", Artist = "Eminem", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Without me.jpg" },
            new Song { Name = "Watermelon Sugar", Url = "https://songstorage25.blob.core.windows.net/songs/Harry Styles - Watermelon Sugar.mp3", Artist = "Harry Styles", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Watermelon sugar.jpg" },
            new Song { Name = "Believer", Url = "https://songstorage25.blob.core.windows.net/songs/Imagine Dragons - Believer.mp3", Artist = "Imagine Dragons", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Believer.jpg" },
            new Song { Name = "Sexy and I Know It", Url = "https://songstorage25.blob.core.windows.net/songs/LMFAO - Sexy and I Know It.mp3", Artist = "LMFAO", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Sexy and i know ti.jpg" },
            new Song { Name = "Judas", Url = "https://songstorage25.blob.core.windows.net/songs/Lady Gaga - Judas.mp3", Artist = "Lady Gaga", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Judas.jpg" },
            new Song { Name = "Uptown Funk", Url = "https://songstorage25.blob.core.windows.net/songs/Mark Ronson - Uptown Funk.mp3", Artist = "Mark Ronson", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Uptown funk.jpg" },
            new Song { Name = "La la la", Url = "https://songstorage25.blob.core.windows.net/songs/Naughty Boy - La la la.mp3", Artist = "Naughty Boy", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/La la la.jpg" },
            new Song { Name = "Smells Like Teen Spirit", Url = "https://songstorage25.blob.core.windows.net/songs/Nirvana - Smells Like Teen Spirit.mp3", Artist = "Nirvana", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Smells like teen spirit.jpg" },
            new Song { Name = "Let Her Go", Url = "https://songstorage25.blob.core.windows.net/songs/Passenger  Let Her Go.mp3", Artist = "Passenger", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/let her go.jpg" },
            new Song { Name = "Sunflower", Url = "https://songstorage25.blob.core.windows.net/songs/Post Malone - Sunflower.mp3", Artist = "Post Malone", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/sunflower.jpg" },
            new Song { Name = "Blank Space", Url = "https://songstorage25.blob.core.windows.net/songs/Taylor Swift - Blank Space.mp3", Artist = "Taylor Swift", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/blank space.jpg" },
            new Song { Name = "Candy Shop", Url = "https://songstorage25.blob.core.windows.net/songs/50 Cent - Candy Shop.mp3", Artist = "50 Cent", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Candy shop.jpg" },
            new Song { Name = "Rolling in the Deep", Url = "https://songstorage25.blob.core.windows.net/songs/Adele - Rolling in the Deep.mp3", Artist = "Adele", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Rolling in the deep.jpg" },
            new Song { Name = "thank u, next", Url = "https://songstorage25.blob.core.windows.net/songs/Ariana Grande - thank u, next.mp3", Artist = "Ariana Grande", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Thank u, next.jpg" },
            new Song { Name = "Pitbull Terrier", Url = "https://songstorage25.blob.core.windows.net/songs/Die Antwoord   Pitbull Terrier.mp3", Artist = "Die Antwoord", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Pitbull terrier.jpg" },
            new Song { Name = "Perfect", Url = "https://songstorage25.blob.core.windows.net/songs/Ed Sheeran - Perfect.mp3", Artist = "Ed Sheeran", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Perfect.jpg" },
            new Song { Name = "Shape of You", Url = "https://songstorage25.blob.core.windows.net/songs/Ed Sheeran - Shape of You.mp3", Artist = "Ed Sheeran", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/Shape of you.jpg" },
            new Song { Name = "The Real Slim Shady", Url = "https://songstorage25.blob.core.windows.net/songs/Eminem - The Real Slim Shady.mp3", Artist = "Eminem", CoverUrl = "https://songstorage25.blob.core.windows.net/covers/The real slim shady.jpg" }
        };
    }
}