using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.LobbyService;
using API.Models;
using System.Security.Claims;
using Services.Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LobbyController : ControllerBase
    {
        private readonly ILobbyService _service;

        public LobbyController(ILobbyService service)
        {
            _service = service;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby([FromBody] LobbyCreateRequest request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var options = new LobbyOptions
            {
                Private = request.Private,
                AiRate = request.AiRate,
                HumanRate = request.HumanRate,
                MaxPlayers = request.MaxPlayers,
                TotalRounds = request.TotalRounds
            };

            var created = await _service.CreateLobbyAsync(userId, options);
            return Ok(created);
        }

        [HttpGet("exists/{code}")]
        public async Task<IActionResult> Exists(string code)
        {
            if (!await _service.LobbyExistsAsync(code))
                return NotFound();

            return Ok();
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<IActionResult> GetUserLobbies()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var response = await _service.GetPlayerLobbiesAsync(userId);

            return Ok(response);
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromBody] int lobbyId)
        {
            try
            {
                var response = await _service.DeleteLobbyAsync(lobbyId);
                return Ok(response);
            }
            catch (Exception ex) when (ex.Message.Contains("Lobby not found"))
            {
                return NotFound("Lobby not found");
            }
        }
    }
}
