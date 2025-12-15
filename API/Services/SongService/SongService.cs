using Services.Models;
using Services.Stores;

namespace Services.SongService
{
    public class SongService : ISongService
    {
        private readonly ISongStore _songStore;
        private readonly Random _random = new Random();

        public SongService(ISongStore songStore)
        {
            _songStore = songStore;
        }

        public IEnumerable<Song> GetAllSongs()
        {
            return _songStore.Songs;
        }

        public Song? GetRandomSong()
        {
            var songs = _songStore.Songs;
            if (songs == null || songs.Count == 0)
                return null;

            int index = _random.Next(songs.Count);
            return songs[index];
        }
    }
}
