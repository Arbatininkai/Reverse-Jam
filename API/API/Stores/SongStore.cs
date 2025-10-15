using API.Models;
using System.Collections.Generic;

namespace API.Stores
{
    public static class SongStore
    {
        public static List<Song> Songs { get; set; } = new List<Song>
        {
            new Song { Name = "Crazy", Url = "https://songstorage25.blob.core.windows.net/songs/Gnarls Barkley - Crazy.mp3", Artist = "Gnarls Barkley" },
            new Song { Name = "Billie Jean", Url = "https://songstorage25.blob.core.windows.net/songs/Michael Jackson - Billie Jean.mp3", Artist = "Michael Jackson" },
            new Song { Name = "Without me", Url = "https://songstorage25.blob.core.windows.net/songs/Eminem - Without Me.mp3", Artist = "Eminem" },
            new Song { Name = "Watermelon Sugar", Url = "https://songstorage25.blob.core.windows.net/songs/Harry Styles - Watermelon Sugar.mp3", Artist = "Harry Styles" },
            new Song { Name = "Believer", Url = "https://songstorage25.blob.core.windows.net/songs/Imagine Dragons - Believer.mp3", Artist = "Imagine Dragons" },
            new Song { Name = "Sexy and I Know It", Url = "https://songstorage25.blob.core.windows.net/songs/LMFAO - Sexy and I Know It.mp3", Artist = "LMFAO" },
            new Song { Name = "Judas", Url = "https://songstorage25.blob.core.windows.net/songs/Lady Gaga - Judas.mp3", Artist = "Lady Gaga" },
            new Song { Name = "Uptown Funk", Url = "https://songstorage25.blob.core.windows.net/songs/Mark Ronson - Uptown Funk.mp3", Artist = "Mark Ronson" },
            new Song { Name = "La la la", Url = "https://songstorage25.blob.core.windows.net/songs/Naughty Boy - La la la.mp3", Artist = "Naughty Boy" },
            new Song { Name = "Smells Like Teen Spirit", Url = "https://songstorage25.blob.core.windows.net/songs/Nirvana - Smells Like Teen Spirit.mp3", Artist = "Nirvana" },
            new Song { Name = "Let Her Go", Url = "https://songstorage25.blob.core.windows.net/songs/Passenger  Let Her Go.mp3", Artist = "Passenger" },
            new Song { Name = "Sunflower", Url = "https://songstorage25.blob.core.windows.net/songs/Post Malone - Sunflower.mp3", Artist = "Post Malone" },
            new Song { Name = "Blank Space", Url = "https://songstorage25.blob.core.windows.net/songs/Taylor Swift - Blank Space.mp3", Artist = "Taylor Swift" },
            new Song { Name = "Candy Shop", Url = "https://songstorage25.blob.core.windows.net/songs/50 Cent - Candy Shop.mp3", Artist = "50 Cent" },
            new Song { Name = "Rolling in the Deep", Url = "https://songstorage25.blob.core.windows.net/songs/Adele - Rolling in the Deep.mp3", Artist = "Adele" },
            new Song { Name = "thank u, next", Url = "https://songstorage25.blob.core.windows.net/songs/Ariana Grande - thank u, next.mp3", Artist = "Ariana Grande" },
            new Song { Name = "Pitbull Terrier", Url = "https://songstorage25.blob.core.windows.net/songs/Die Antwoord   Pitbull Terrier.mp3", Artist = "Die Antwoord" },
            new Song { Name = "Perfect", Url = "https://songstorage25.blob.core.windows.net/songs/Ed Sheeran - Perfect.mp3", Artist = "Ed Sheeran" },
            new Song { Name = "Shape of You", Url = "https://songstorage25.blob.core.windows.net/songs/Ed Sheeran - Shape of You.mp3", Artist = "Ed Sheeran" },
            new Song { Name = "The Real Slim Shady", Url = "https://songstorage25.blob.core.windows.net/songs/Eminem - The Real Slim Shady.mp3", Artist = "Eminem" }
        };
    }
}