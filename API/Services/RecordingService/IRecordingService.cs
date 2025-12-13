using Microsoft.AspNetCore.Http;
using Services.Models;

namespace Services.RecordingService
{
    public interface IRecordingService
    {
        Task<RecordingDto> UploadRecordingAsync(int lobbyId, int roundIndex, int userId, RecordingUploadRequest request, string baseUrl);
        Task<IEnumerable<RecordingDto>> GetRecordingsAsync(string lobbyCode, int userId);
    }
}
