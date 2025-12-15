using Integrations.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Services.AiScoringService;
using Services.Hubs;
using Services.Models;
using Services.Stores;
using Xabe.FFmpeg;

namespace Services.RecordingService;

public class RecordingService : IRecordingService
{
    private readonly IHubContext<LobbyHub> _hubContext;
    private readonly AppDbContext _db;
    private readonly IAIScoringService _scoringService;

    public RecordingService(
        IHubContext<LobbyHub> hubContext,
        AppDbContext db,
        IAIScoringService scoringService)
    {
        _hubContext = hubContext;
        _db = db;
        _scoringService = scoringService;
    }

    public async Task<RecordingDto> UploadRecordingAsync(int lobbyId, int roundIndex, int userId, RecordingUploadRequest request, string baseUrl)
    {
        if (request.File == null || request.File.Length == 0)
            throw new ArgumentException("File is required");

        var lobby = await _db.Lobbies
            .Include(l => l.Recordings)
            .Include(l => l.Players)
            .FirstOrDefaultAsync(l => l.Id == lobbyId);

        if (lobby == null)
            throw new InvalidOperationException("Lobby not found");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        if (!lobby.Players.Any(p => p.Id == user.Id))
            throw new UnauthorizedAccessException("User is not a participant");

        var servicesRoot = Path.Combine(Directory.GetCurrentDirectory(), "..", "Services");
        var folder = Path.Combine(servicesRoot, "recordings", lobby.LobbyCode);
        Directory.CreateDirectory(folder);

        var extension = Path.GetExtension(request.File.FileName);
        var filename = $"{user.Id}_{Guid.NewGuid()}{extension}";
        var path = Path.Combine(folder, filename);

        await using (var stream = new FileStream(path, FileMode.Create))
            await request.File.CopyToAsync(stream);

        try
        {
            await ReverseAudioFileAsync(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio reversal failed: {ex.Message}");
        }

        var url = $"{baseUrl}/Services/recordings/{lobby.LobbyCode}/{filename}";

        var recordingEntity = new RecordingEntity
        {
            UserId = user.Id,
            User = user,
            FileName = filename,
            Url = url,
            UploadedAt = DateTime.UtcNow,
            Round = roundIndex + 1,
            LobbyId = lobby.Id,
        };

        if (lobby.AiRate)
        {
         
                var response = await _scoringService.ScoreRecordingAsync(request.OriginalSongLyrics ?? "", path);
                recordingEntity.AiScore = response.SimilarityScore;
                recordingEntity.StatusMessage = response.TranscribedText;
           
        }

        lobby.Recordings.Add(recordingEntity);
        _db.Recordings.Add(recordingEntity);
        await _db.SaveChangesAsync();

        var lobbyDto = new LobbyDto
        {
            Id = lobby.Id,
            LobbyCode = lobby.LobbyCode,
            AiRate = lobby.AiRate,
            HumanRate = lobby.HumanRate,
            MaxPlayers = lobby.MaxPlayers,
            TotalRounds = lobby.TotalRounds,
            OwnerId = lobby.OwnerId,
            HasGameStarted = lobby.HasGameStarted,
            CurrentRound = lobby.CurrentRound,
            CurrentPlayerIndex = lobby.CurrentPlayerIndex,
            Recordings = lobby.Recordings.Select(r => new RecordingDto
            {
                Id = r.Id,
                Url = r.Url,
                UserId = r.UserId,
                FileName = r.FileName,
                UploadedAt = r.UploadedAt,
                Round = r.Round,
                AiScore = r.AiScore,
                StatusMessage = r.StatusMessage
            }).ToList(),
            Players = lobby.Players.Select(p => new UserDto
            {
                Id = p.Id,
                Name = p.Name ?? "user",
                Email = p.Email ?? "user@gmail.com",
                PhotoUrl = p.PhotoUrl ?? "",
                Emoji = p.Emoji ?? ""
            }).ToList()
        };

        await _hubContext.Clients.Group(lobby.LobbyCode).SendAsync("LobbyUpdated", lobbyDto);


        return new RecordingDto
        {
            Id = recordingEntity.Id,
            Url = recordingEntity.Url,
            UserId = recordingEntity.UserId,
            FileName = recordingEntity.FileName,
            UploadedAt = recordingEntity.UploadedAt,
            Round = recordingEntity.Round,
            LobbyId = recordingEntity.LobbyId,
            AiScore = recordingEntity.AiScore,
            StatusMessage = recordingEntity.StatusMessage
        };
    }

    public async Task<IEnumerable<RecordingDto>> GetRecordingsAsync(string lobbyCode, int userId)
    {
        var lobby = await _db.Lobbies
            .Include(l => l.Recordings)
            .Include(l => l.Players)
            .FirstOrDefaultAsync(l => l.LobbyCode == lobbyCode);

        if (lobby == null)
            throw new InvalidOperationException("Lobby not found");

        if (lobby.Private && !lobby.Players.Any(p => p.Id == userId))
            throw new UnauthorizedAccessException("Not allowed");

        return lobby.Recordings
            .OrderBy(r => r.Round)
            .Select(r => new RecordingDto
            {
                UserId = r.UserId,
                FileName = r.FileName,
                Url = r.Url,
                UploadedAt = r.UploadedAt,
                Round = r.Round,
                AiScore = r.AiScore,
                LobbyId = r.LobbyId,
            });
    }


    private async Task ReverseAudioFileAsync(string inputPath)
    {
        var folder = Path.GetDirectoryName(inputPath)!;
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var output = Path.Combine(folder, name + "_reversed.m4a");

        try
        {
            var conversion = FFmpeg.Conversions.New()
                .AddParameter("-i \"" + inputPath + "\"", ParameterPosition.PreInput)
                .AddParameter("-af areverse")
                .AddParameter("-c:a aac")
                .SetOutput(output);

            await conversion.Start();

            if (File.Exists(output))
            {
                File.Delete(inputPath);
                File.Move(output, inputPath);
            }
        }
        catch (Xabe.FFmpeg.Exceptions.ConversionException)
        {
            try { if (File.Exists(output)) File.Delete(output); } catch { }
        }
        catch (Exception)
        {
            try { if (File.Exists(output)) File.Delete(output); } catch { }
        }
    }
}
