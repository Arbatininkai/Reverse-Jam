using Services.Models;


namespace Services.SongService
{
    public interface ISongService
    {
        public IEnumerable<Song> GetAllSongs();
        public Song? GetRandomSong();
    }
}
