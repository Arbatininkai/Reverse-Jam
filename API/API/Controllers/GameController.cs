
using API.Data;
using API.Hubs;
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IHubContext<LobbyHub> _hubContext;
        private readonly AppDbContext _dbContext;

        public GameController(IHubContext<LobbyHub> hubContext, AppDbContext dbContext)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        [Authorize]
        [HttpPost("submit-votes")]
        public async Task<IActionResult> SubmitVotes([FromBody] EndRoundRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.LobbyCode))
                return BadRequest("LobbyCode is required.");

            var lobby = await _dbContext.Lobbies.FirstOrDefaultAsync(l => l.LobbyCode == request.LobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

           
            var lobbyScores = LobbyStore.GetOrCreateLobbyScores(request.LobbyCode);
            lobbyScores.AddVotes(request.Votes, request.Round);

            return Ok(new { message = "Votes submitted" });
        }

        [Authorize]
        [HttpPost("calculate-final-scores")]
        public async Task<IActionResult> CalculateFinalScores([FromBody] string lobbyCode)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized("No user ID found in token.");

            int userId = int.Parse(userIdStr);
            var user = await _dbContext.Users.FindAsync(userId);

            var lobby = await _dbContext.Lobbies.FirstOrDefaultAsync(l => l.LobbyCode == lobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

            var lobbyScores = LobbyStore.GetOrCreateLobbyScores(lobbyCode);
            var finalScores = lobbyScores.GetFinalScores();

            var winnerId = finalScores.Select(p => p.UserId).FirstOrDefault();
            var winner = _dbContext.Users.FirstOrDefault(u => u.Id == winnerId);

            if (winner != null && winner == user)
            {
                winner.TotalWins++;

                _dbContext.Update(winner);
                await _dbContext.SaveChangesAsync();
            }
            await _hubContext.Clients.Group(lobbyCode).SendAsync("PlayerWon", winner);

            return Ok(new { scores = finalScores });
        }
    }
}