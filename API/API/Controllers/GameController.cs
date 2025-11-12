
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        [HttpPost("submit-votes")]
        public IActionResult SubmitVotes([FromBody] EndRoundRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.LobbyCode))
                return BadRequest("LobbyCode is required.");

            var lobby = LobbyStore.Lobbies.FirstOrDefault(l => l.LobbyCode == request.LobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

           
            var lobbyScores = LobbyStore.GetOrCreateLobbyScores(request.LobbyCode);
            lobbyScores.AddVotes(request.Votes, request.Round);

            return Ok(new { message = "Votes submitted" });
        }

        [HttpPost("calculate-final-scores")]
        public IActionResult CalculateFinalScores([FromBody] string lobbyCode)
        {
            var lobby = LobbyStore.Lobbies.FirstOrDefault(l => l.LobbyCode == lobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

            var lobbyScores = LobbyStore.GetOrCreateLobbyScores(lobbyCode);
            var finalScores = lobbyScores.GetFinalScores();

            var winnerId = finalScores.Select(p => p.UserId).FirstOrDefault();
            var winner = UserStore.Users.FirstOrDefault(u => u.Id == winnerId);

            if (winner != null)
            {
                winner.TotalWins++;

                //TODO: Update the database
                //UserStore.UpdateUser(winner);
            }

            return Ok(new { scores = finalScores });
        }
    }
}