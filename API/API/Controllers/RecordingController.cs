using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.RecordingService;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecordingsController : ControllerBase
    {
        private readonly IRecordingService _svc;

        public RecordingsController(IRecordingService svc)
        {
            _svc = svc;
        }

        [Authorize]
        [HttpPost("upload/{lobbyId}/{roundIndex}")]
        public async Task<IActionResult> Upload(int lobbyId, int roundIndex, [FromForm] RecordingUploadRequest req)
        {
            if (req?.File == null || req.File.Length == 0)
                return BadRequest("File is required");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            try
            {
                var serviceReq = new Services.Models.RecordingUploadRequest
                {
                    File = req.File,
                    OriginalSongLyrics = req.OriginalSongLyrics
                };
                var result = await _svc.UploadRecordingAsync(lobbyId, roundIndex, userId, serviceReq, baseUrl);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Authorize]
        [HttpGet("{lobbyCode}/recordings")]
        public async Task<IActionResult> GetAll(string lobbyCode)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            try
            {
                var recordings = await _svc.GetRecordingsAsync(lobbyCode, userId);
                return Ok(recordings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
