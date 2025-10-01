using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        [Authorize] // tik prisijungę per Google gali kurti lobby
        [HttpPost("create")]
        public IActionResult CreateLobby([FromBody] Lobby? options)
        {
            if (options == null)
                return BadRequest("Options required");

            // paimam prisijungusio user info iš tokeno
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var creator = UserStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            var newLobby = new Lobby
            {
                Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                Private = options.Private, //is frontendo gaunamos reiksmes
                AiRate = options.AiRate,
                HumanRate = options.HumanRate
            };

            if (creator != null)
                newLobby.Players.Add(creator);

            LobbyStore.Lobbies.Add(newLobby);

            return Ok(newLobby);
        }
    }
}
