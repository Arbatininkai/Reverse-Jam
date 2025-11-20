
using API.Hubs;
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {

        private readonly IHubContext<LobbyHub> _hubContext;
         private readonly ILobbyStore _lobbyStore;

        public GameController(IHubContext<LobbyHub> hubContext, ILobbyStore lobbyStore)
        {
            _hubContext = hubContext;
            _lobbyStore = lobbyStore;
        }


        [HttpPost("submit-votes")]
        public IActionResult SubmitVotes([FromBody] EndRoundRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.LobbyCode))
                return BadRequest("LobbyCode is required.");


            var lobby = _lobbyStore.Lobbies.Values.FirstOrDefault(l => l.LobbyCode == request.LobbyCode);

            if (lobby is null)
                return NotFound("Lobby not found.");

           
            var lobbyScores = _lobbyStore.GetOrCreateLobbyScores(request.LobbyCode);
            lobbyScores.AddVotes(request.Votes, request.Round);

            return Ok(new { message = "Votes submitted" });
        }

        [HttpPost("calculate-final-scores")]
        public async Task<IActionResult> CalculateFinalScores([FromBody] string lobbyCode)
        {
            var lobby = _lobbyStore.Lobbies.Values.FirstOrDefault(l => l.LobbyCode == lobbyCode);
            if (lobby is null)
                return NotFound("Lobby not found.");

            var lobbyScores = _lobbyStore.GetOrCreateLobbyScores(lobbyCode);
            var finalScores = lobbyScores.GetFinalScores();

            var winnerId = finalScores.Select(p => p.UserId).FirstOrDefault();
            var winner = UserStore.Users.FirstOrDefault(u => u.Id == winnerId);

            if (winner != null)
            {
                winner.TotalWins++;

                //TODO: Update the database
                //UserStore.UpdateUser(winner);
            }
            await _hubContext.Clients.Group(lobby.LobbyCode).SendAsync("PlayerWon", winner);


            return Ok(new { scores = finalScores });
        }
    }
}