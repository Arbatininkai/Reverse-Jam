using API.Hubs;
using API.Models;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordingsController : ControllerBase
    {
        private readonly IHubContext<LobbyHub> _hubContext;
        private readonly ILobbyStore _lobbyStore;
        private readonly IUserStore _userStore;


        public RecordingsController(IHubContext<LobbyHub> hubContext, ILobbyStore lobbyStore, IUserStore userStore)
        {
            _hubContext = hubContext;
            _lobbyStore = lobbyStore;
            _userStore = userStore;
        }

        [Authorize]
        [HttpPost("upload/{lobbyId}/{roundIndex}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRecording(int lobbyId, int roundIndex, [FromForm] RecordingUploadRequest request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var lobby = _lobbyStore.Lobbies.FirstOrDefault(l => l.Id == lobbyId);
            if (lobby == null)
                return NotFound("Lobby not found");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _userStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);
            if (user == null)
                return Unauthorized("User not found");

            if (!lobby.Players.Any(p => p.Id == user.Id))
                return Forbid("User is not a participant of this lobby");

            var recordingsFolder = Path.Combine(Directory.GetCurrentDirectory(), "recordings", lobby.LobbyCode);
            Directory.CreateDirectory(recordingsFolder);

            while (lobby.RecordingsByRound.Count <= roundIndex)
                lobby.RecordingsByRound.Add(new List<Recording>());

            var existing = lobby.RecordingsByRound[roundIndex].FirstOrDefault(r => r.UserId == user.Id);
            if (existing != null)
            {
                try
                {
                    var oldPath = Path.Combine(recordingsFolder, existing.FileName);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
                catch { }
            }

            var extension = Path.GetExtension(file.FileName);
            var storedFileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(recordingsFolder, storedFileName);

            try
            {
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
            }
            catch
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to save file");
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/api/recordings/{lobby.LobbyCode}/{storedFileName}";

            var recording = new Recording
            {
                UserId = user.Id,
                FileName = storedFileName,
                Url = fileUrl,
                UploadedAt = DateTime.UtcNow
            };

            if (existing != null)
            {
                existing.FileName = recording.FileName;
                existing.Url = recording.Url;
                existing.UploadedAt = recording.UploadedAt;
            }
            else
            {
                lobby.RecordingsByRound[roundIndex].Add(recording);
            }
            await _hubContext.Clients.Group(lobby.LobbyCode).SendAsync("LobbyUpdated", lobby);

            return Ok(recording);
        }


        [HttpGet("{lobbyCode}/{fileName}")]
        public IActionResult GetRecording(string lobbyCode, string fileName)
        {
            var lobby = _lobbyStore.Lobbies.FirstOrDefault(l =>
                l.LobbyCode.Equals(lobbyCode, StringComparison.OrdinalIgnoreCase));
            if (lobby == null)
                return NotFound("Lobby not found");

            if (lobby.Private)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = _userStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user == null || !lobby.Players.Any(p => p.Id == user.Id))
                    return Forbid();
            }

            var recordingsFolder = Path.Combine(Directory.GetCurrentDirectory(), "recordings", lobby.LobbyCode);
            var filePath = Path.Combine(recordingsFolder, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
                contentType = "application/octet-stream";

            return PhysicalFile(filePath, contentType);
        }

        [Authorize]
        [HttpGet("{lobbyCode}/recordings")]
        public IActionResult GetAllRecordings(string lobbyCode)
        {
            var lobby = _lobbyStore.Lobbies.FirstOrDefault(l =>
                l.LobbyCode.Equals(lobbyCode, StringComparison.OrdinalIgnoreCase));
            if (lobby == null)
                return NotFound("Lobby not found");

            if (lobby.Private)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = _userStore.Users.FirstOrDefault(u => u.Id.ToString() == userId);
                if (user == null || !lobby.Players.Any(p => p.Id == user.Id))
                    return Forbid();
            }

            var result = lobby.RecordingsByRound
                .SelectMany((roundList, roundIndex) => roundList.Select(r => new
                {
                    r.UserId,
                    r.FileName,
                    r.Url,
                    r.UploadedAt,
                    Round = roundIndex + 1
                }))
                .ToList();

            return Ok(result);
        }
    }
}