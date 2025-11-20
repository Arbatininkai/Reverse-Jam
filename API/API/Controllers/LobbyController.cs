using System.Security.Claims;
using System.Text.RegularExpressions;
using API.Hubs;
using API.Models;
using API.Services;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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

        public LobbyController(IHubContext<LobbyHub> hubContext, ILobbyStore lobbyStore, IUserStore userStore, IRandomValue randomValue)
        {
            _hubContext = hubContext;
            _lobbyStore = lobbyStore;
            _userStore = userStore;
            _randomValue = randomValue;
        }

        //Lobby sukurimas
        [Authorize]
        [HttpPost("create")]
        public IActionResult CreateLobby([FromBody] Lobby? options)
        {
            if (options == null)
                return BadRequest("Options required");

            // paimam prisijungusio user info iš tokeno
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var creator = _userStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (creator == null)
            {
                return BadRequest("User does not exist");
            }

            var newLobby = new Lobby
            {
                Id = _lobbyStore.Lobbies.Count > 0 ? _lobbyStore.Lobbies.Values.Max(l => l.Id) + 1 : 1,
                Private = options.Private, //is frontendo gaunamos reiksmes
                AiRate = options.AiRate,
                TotalRounds = options.TotalRounds,
                HumanRate = options.HumanRate,
                OwnerId = creator.Id
            };

            newLobby.Players.Add(creator);

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
        [HttpPost("play")]
        public IActionResult Play([FromBody] PlayRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (user == null)
                return Unauthorized("User not found");

            if (!string.IsNullOrEmpty(request.LobbyCode)) //jeigu ne null tai iveda seed
            {
                var lobby = _lobbyStore.Lobbies.Values.FirstOrDefault(l =>
                    string.Equals(l.LobbyCode.ToString(), request.LobbyCode, StringComparison.OrdinalIgnoreCase));

                if (lobby == null)
                    return NotFound("Lobby not found");

                if (lobby.Players.Count >= lobby.MaxPlayers)
                    return BadRequest("Lobby is full");

                if (!lobby.Players.Any(p => p.Id == user.Id))
                    lobby.Players.Add(user);

                return Ok(lobby);
            }

            var availableLobbies = _lobbyStore.Lobbies //jeigu nenurodyta nieko tai i random meta lobby
                .Values.Where(l => !l.Private && l.Players.Count < l.MaxPlayers)
                .ToList();

            if (availableLobbies.Count == 0)
            {
                var newLobby = new Lobby
                {
                    Id = _lobbyStore.Lobbies.Count > 0 ? _lobbyStore.Lobbies.Values.Max(l => l.Id) + 1 : 1,
                    Private = false,
                    MaxPlayers = 4
                };
                newLobby.Players.Add(user);
                _lobbyStore.Lobbies.TryAdd(newLobby.Id, newLobby);
                return Ok(newLobby);
            }

             int maxPlayersNow = 0;
                 foreach (var lobby in availableLobbies)
                 {
                     if (lobby.Players.Count > maxPlayersNow)
                         maxPlayersNow = lobby.Players.Count;
                 }

            var bestLobbies = availableLobbies.Where(l => l.Players.Count == maxPlayersNow).ToList(); //randam labiausiai uzpildyta lobby

           
            var chosenLobby = bestLobbies[_randomValue.Next(bestLobbies.Count)]; //isrenkam random jei yra keli

            if (!chosenLobby.Players.Any(p => p.Id == user.Id)) //apsauga jeigu tas pats useris bando i ta pati lobby eiti
                chosenLobby.Players.Add(user);

            return Ok(chosenLobby);
        }

        [Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLobby([FromBody] int lobbyId)
        {
            var lobby = _lobbyStore.Lobbies.Values.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found");

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

            // Remove the lobby
            _lobbyStore.Lobbies.TryRemove(lobby.Id, out _);

            return Ok(new { message = "Lobby deleted successfully" });
        }
     
    }
}
