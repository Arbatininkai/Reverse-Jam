
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
        private readonly ILobbyStore _lobbyStore;

        public GameController(ILobbyStore lobbyStore)
        {
            _lobbyStore = lobbyStore;
        }
        [HttpPost("submit-votes")]
        public IActionResult SubmitVotes([FromBody] EndRoundRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.LobbyCode))
                return BadRequest("LobbyCode is required.");

            var lobby = _lobbyStore.Lobbies.FirstOrDefault(l => l.LobbyCode == request.LobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

           
            var lobbyScores = _lobbyStore.GetOrCreateLobbyScores(request.LobbyCode);
            lobbyScores.AddVotes(request.Votes, request.Round);

            return Ok(new { message = "Votes submitted" });
        }

        [HttpPost("calculate-final-scores")]
        public IActionResult CalculateFinalScores([FromBody] string lobbyCode)
        {
            var lobby = _lobbyStore.Lobbies.FirstOrDefault(l => l.LobbyCode == lobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

            var lobbyScores = _lobbyStore.GetOrCreateLobbyScores(lobbyCode);
            var finalScores = lobbyScores.GetFinalScores();

            return Ok(new { scores = finalScores });
        }
    }
}