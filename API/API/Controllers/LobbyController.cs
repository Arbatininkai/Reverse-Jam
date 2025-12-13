using API.Data;
using API.Hubs;
using API.Models;
using API.Services;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        private readonly IHubContext<LobbyHub> _hubContext;
        private readonly ILobbyStore _lobbyStore;
        private readonly IUserStore _userStore;
        private readonly IRandomValue _randomValue;
        private readonly AppDbContext _dbContext;

        public LobbyController(IHubContext<LobbyHub> hubContext, ILobbyStore lobbyStore, IUserStore userStore, IRandomValue randomValue, AppDbContext dbContext)
        {
            _hubContext = hubContext;
            _lobbyStore = lobbyStore;
            _userStore = userStore;
            _randomValue = randomValue;
            _dbContext = dbContext;
        }

        //Lobby sukurimas
        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby([FromBody] Lobby? options)
        {
            if (options == null)
                return BadRequest("Options required");

            // paimam prisijungusio user info iš tokeno
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var creator = await _dbContext.Users.FindAsync(int.Parse(userId!));

            if (creator == null)
            {
                return BadRequest("User does not exist");
            }

            var newLobby = new Lobby
            {
                Private = options.Private,
                AiRate = options.AiRate,
                TotalRounds = options.TotalRounds,
                HumanRate = options.HumanRate,
                OwnerId = creator.Id,
            };
            newLobby.Players.Add(creator);
            _dbContext.Lobbies.Add(newLobby);
            await _dbContext.SaveChangesAsync();

            LobbyStore.LobbiesDict.TryAdd(newLobby.Id, newLobby);

            return Ok(newLobby);
        }

        [HttpGet("exists/{code}")]
        public async Task<IActionResult> LobbyExists(string code)
        {
            var exists = await _dbContext.Lobbies.AnyAsync(l =>
                l.LobbyCode.ToLower() == code.ToLower());

            if (!exists) return NotFound("Lobby not found");
            return Ok();
        }

        [Authorize]
        [HttpGet("get-player-lobbies")]
        public async Task<IActionResult> GetPlayerLobbies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _dbContext.Users.FindAsync(int.Parse(userId!));
            if (user == null)
            {
                return BadRequest("User does not exist");
            }
            var lobbies = await _dbContext.Lobbies
                .Include(l => l.Players)
                .Where(l => l.Players.Any(p => p.Id == user.Id))
                .ToListAsync();

            if (!lobbies.Any())
                return Ok(new List<object>());

            var lobbyScoresList = lobbies
                .Select(l => new
                {
                    Lobby = l,
                    Scores = _lobbyStore.GetLobbyScores(l.LobbyCode)
                })
                .ToList();

            return Ok(lobbyScoresList);
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLobby([FromBody] int lobbyId)
        {
            var lobby = await _dbContext.Lobbies.FirstOrDefaultAsync(l => l.Id == lobbyId);

            if (lobby == null)
                return NotFound("Lobby not found");

            _dbContext.Lobbies.Remove(lobby);

            await _dbContext.SaveChangesAsync();

            LobbyStore.LobbiesDict.TryRemove(lobby.Id, out _);

            try
            {
                var recordingsPath = Path.Combine(Directory.GetCurrentDirectory(), "recordings");
                if (Directory.Exists(recordingsPath))
                {
                    Directory.Delete(recordingsPath, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up recordings : {ex.Message}");
            }

          
            await _hubContext.Clients.Group((lobby.LobbyCode).ToString()).SendAsync("LobbyDeleted");

            return Ok(new { message = "Lobby deleted successfully" });
        }
    }
}