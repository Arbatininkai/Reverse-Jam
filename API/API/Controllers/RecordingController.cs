using API.Data;
using API.Hubs;
using API.Models;
using API.Services;
using API.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Xabe.FFmpeg;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordingsController : ControllerBase
    {
        private readonly IHubContext<LobbyHub> _hubContext;
        private readonly ILobbyStore _lobbyStore;
        private readonly IUserStore _userStore;
        private readonly AppDbContext _dbContext;
        
        //private readonly AIScoringService _scoringService;


        public RecordingsController(IHubContext<LobbyHub> hubContext, ILobbyStore lobbyStore, IUserStore userStore, AppDbContext dbContext)
        {
            _hubContext = hubContext;
            _lobbyStore = lobbyStore;
            _userStore = userStore;
             _dbContext = dbContext;
           // _scoringService = scoringService;
        }

        [Authorize]
        [HttpPost("upload/{lobbyId}/{roundIndex}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRecording(int lobbyId, int roundIndex, [FromForm] RecordingUploadRequest request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var lobby = await _dbContext.Lobbies
                .Include(l => l.Recordings)
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.Id == lobbyId);

            if (lobby == null)
                return NotFound("Lobby not found");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var uid))
                return Unauthorized("Invalid user token");

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == uid);

            if (user == null)
            {
                Console.WriteLine("ERROR: User not found in DB, but token exists.");
                return Unauthorized("User not found");
            }


            if (!lobby.Players.Any(p => p.Id == user.Id))
                return Forbid("User is not a participant of this lobby");

            var recordingsFolder = Path.Combine(Directory.GetCurrentDirectory(), "recordings", lobby.LobbyCode);
            Directory.CreateDirectory(recordingsFolder);

            var round = roundIndex + 1;
          

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
            
            // Reverse the audio file after saving
            try
            {
                await ReverseAudioFileAsync(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio reversal failed: {ex.Message}");
            }


            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fileUrl = $"{baseUrl}/api/recordings/{lobby.LobbyCode}/{storedFileName}";

            var recording = new Recording
            {
                UserId = user.Id,
                User = user,
                FileName = storedFileName,
                Url = fileUrl,
                UploadedAt = DateTime.UtcNow,
                Round = round,
                Lobby = lobby,
                LobbyId = lobbyId,
            };

            lobby.Recordings.Add(recording);
            _dbContext.Recordings.Add(recording);
            await _dbContext.SaveChangesAsync();
            
            await _hubContext.Clients.Group(lobby.LobbyCode).SendAsync("LobbyUpdated", lobby);

            return Ok(recording);
        }


        [HttpGet("{lobbyCode}/{fileName}")]
        public IActionResult GetRecording(string lobbyCode, string fileName)
        {
            var lobby = _lobbyStore.Lobbies.Values.FirstOrDefault(l =>
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
        public async Task<IActionResult> GetAllRecordings(string lobbyCode)
        {
            var lobby = await _dbContext.Lobbies
                .Include(l => l.Recordings)
                .Include(l => l.Players)
                .FirstOrDefaultAsync(l => l.LobbyCode == lobbyCode);

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

            var result = lobby.Recordings
                .GroupBy(r => r.Round)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.Select(r => new
                {
                    r.UserId,
                    r.FileName,
                    r.Url,
                    r.UploadedAt,
                    r.Round
                }))
                .ToList();


            return Ok(result);
        }
        private async Task ReverseAudioFileAsync(string inputPath)
        {
            var directory = Path.GetDirectoryName(inputPath)!;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            var outputPath = Path.Combine(directory, fileNameWithoutExt + "_reversed.m4a");

            var conversion = FFmpeg.Conversions.New()
                .AddParameter("-i \"" + inputPath + "\"", ParameterPosition.PreInput)   // input
                .AddParameter("-af areverse")                                          // apply filter
                .AddParameter("-c:a aac")                                              // ensure M4A codec
                .SetOutput(outputPath);

            await conversion.Start();

            System.IO.File.Delete(inputPath);
            System.IO.File.Move(outputPath, inputPath);
        }

    }
}