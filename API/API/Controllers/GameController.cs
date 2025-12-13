using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Services.GameService.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GameController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [Authorize]
        [HttpPost("submit-votes")]
        public async Task<IActionResult> SubmitVotes([FromBody] EndRoundRequest request)
        {
            try
            {
                await _gameService.SubmitVotesAsync(request);
                return Ok(new { message = "Votes submitted" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("calculate-final-scores")]
        public async Task<IActionResult> CalculateFinalScores([FromBody] string lobbyCode)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized("Invalid user ID.");

            try
            {
                var result = await _gameService.CalculateFinalScoresAsync(lobbyCode, userId);
                return Ok(new { scores = result.Scores, winner = result.Winner });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
