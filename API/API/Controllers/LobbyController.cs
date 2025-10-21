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

        [Authorize]
        [HttpPost("play")]
        public IActionResult Play([FromBody] PlayRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = UserStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (user == null)
                return Unauthorized("User not found");

            if (!string.IsNullOrEmpty(request.LobbyCode)) //jeigu ne null tai iveda seed
            {
                var lobby = LobbyStore.Lobbies.FirstOrDefault(l =>
                    l.LobbyCode.Equals(request.LobbyCode, StringComparison.OrdinalIgnoreCase));

                if (lobby == null)
                    return NotFound("Lobby not found");

                if (lobby.Players.Count >= lobby.MaxPlayers)
                    return BadRequest("Lobby is full");

                if (!lobby.Players.Any(p => p.Id == user.Id))
                    lobby.Players.Add(user);

                return Ok(lobby);
            }

            var availableLobbies = LobbyStore.Lobbies //jeigu nenurodyta nieko tai i random meta lobby
                .Where(l => !l.Private && l.Players.Count < l.MaxPlayers)
                .ToList();

            if (availableLobbies.Count == 0)
            {
                var newLobby = new Lobby
                {
                    Id = LobbyStore.Lobbies.Count > 0 ? LobbyStore.Lobbies.Max(l => l.Id) + 1 : 1,
                    Private = false,
                    MaxPlayers = 4
                };
                newLobby.Players.Add(user);
                LobbyStore.Lobbies.Add(newLobby);
                return Ok(newLobby);
            }

            int maxPlayersNow = availableLobbies.Max(l => l.Players.Count);
            var bestLobbies = availableLobbies.Where(l => l.Players.Count == maxPlayersNow).ToList(); //randam labiausiai uzpildyta lobby

            var random = new Random();
            var chosenLobby = bestLobbies[random.Next(bestLobbies.Count)]; //isrenkam random jei yra keli

            if (!chosenLobby.Players.Any(p => p.Id == user.Id)) //apsauga jeigu tas pats useris bando i ta pati lobby eiti
                chosenLobby.Players.Add(user);

            return Ok(chosenLobby);
        }


       
    }
}
