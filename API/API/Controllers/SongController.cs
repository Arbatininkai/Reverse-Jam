using System;
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController : ControllerBase
    {
        [HttpGet]  // GET api/songs
        public IActionResult GetSongs()
        {
            return Ok(SongStore.Songs);
        }

        [HttpGet("random")]
        public IActionResult GetSong()
        {
            if (SongStore.Songs == null || SongStore.Songs.Count == 0)
                return NotFound("No songs available");

            var random = Random.Shared;
            var song = SongStore.Songs[random.Next(SongStore.Songs.Count)];
            return Ok(song);
        }
    }
}
