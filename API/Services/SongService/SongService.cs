using Services.Models;
using Services.Stores;
using Services.Utils;

namespace Services.SongService
{
    public class SongService : ISongService
    {
        private readonly ISongStore _songStore;
        private readonly IRandomValue _random;

        public SongService(ISongStore songStore, IRandomValue random)
        {
            _songStore = songStore;
            _random = random;
        }

        public IEnumerable<Song> GetAllSongs()
        {
            return _songStore.Songs;
        }

        public Song? GetRandomSong()
        {
            if (_songStore.Songs == null || _songStore.Songs.Count == 0)
                return null;

            var index = _random.Next(_songStore.Songs.Count);
            return _songStore.Songs[index];
        }
    }
}
