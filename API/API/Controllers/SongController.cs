using System;
using API.Models;
using API.Services;
using API.Stores;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController : ControllerBase
    {
        private readonly ISongStore _songStore;
        private readonly IRandomValue _randomValue;
        public SongsController(ISongStore songStore, IRandomValue randomValue)
        {
            _songStore = songStore;
            _randomValue = randomValue;
        }

        [HttpGet] // GET api/songs
        public IActionResult GetSongs() => Ok(_songStore.Songs);

        [HttpGet("random")] // GET api/songs/random
        public IActionResult GetRandomSong()
        {
            if (_songStore.Songs == null || _songStore.Songs.Count == 0)
                return NotFound("No songs available");

            var song = _songStore.Songs[_randomValue.Next(_songStore.Songs.Count)];
            return Ok(song);
        }
    }
}
