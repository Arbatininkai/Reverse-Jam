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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public LobbyController(IHubContext<LobbyHub> hubContext, ILobbyStore lobbyStore, IUserStore userStore, IRandomValue randomValue,AppDbContext dbContext)
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

            _lobbyStore.Lobbies.TryAdd(newLobby.Id, newLobby);

            return Ok(newLobby);
        }

        [HttpGet("exists/{code}")]
        public IActionResult LobbyExists(string code)
        {
            var exists = _lobbyStore.Lobbies.Values.Any(l =>
                string.Equals(l.LobbyCode.ToString(), code, StringComparison.OrdinalIgnoreCase));
            if (!exists) return NotFound("Lobby not found");
            return Ok();
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


            // Remove the lobby in memory
            LobbyStore.Lobbies.TryRemove(lobby.Id, out _);

/*
        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLobby([FromBody] int lobbyId)
        {
            var lobby = _lobbyStore.Lobbies.Values.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found");
*/
//not sure what to do abaut this

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

            // Notify all players that the lobby is deleted
            await _hubContext.Clients.Group((lobby.LobbyCode).ToString()).SendAsync("LobbyDeleted");

            return Ok(new { message = "Lobby deleted successfully" });
        }
     
    }
}