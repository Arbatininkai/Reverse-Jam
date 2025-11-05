using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        // POST api/game/end-round
        [HttpPost("end-round")]
        public IActionResult EndRound([FromBody] EndRoundRequest request)
        {
            if (request is null || request.LobbyCode is null)
                return BadRequest("LobbyCode and request body are required.");

            var lobby = LobbyStore.Lobbies
                .FirstOrDefault(l => l.LobbyCode == request.LobbyCode);

            if (lobby is null)
                return NotFound("Lobby not found.");

            var lobbyUserIds = lobby.Players.Select(p => p.Id).ToHashSet();

            var filteredVotes = (request.Votes ?? Enumerable.Empty<VoteDto>())
                .Where(v => lobbyUserIds.Contains(v.TargetUserId));

            var scores = filteredVotes
                .GroupBy(v => v.TargetUserId)
                .Select(g => new ScoreEntry
                {
                    UserId = g.Key,
                    Name = lobby.Players.FirstOrDefault(p => p.Id == g.Key)?.Name,
                    Score = g.Sum(x => x.Score)
                })
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.Name)
                .ToList();

            return Ok(new
            {
                lobby = new { lobby.LobbyCode, lobby.Id },
                scores
            });
        }
    }
}