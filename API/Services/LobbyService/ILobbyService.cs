using Services.Models;

namespace Services.LobbyService
{
    public interface ILobbyService
    {
        Task<LobbyDto> CreateLobbyAsync(int creatorId, LobbyOptions options);
        Task<bool> LobbyExistsAsync(string code);
        Task<IEnumerable<LobbyWithScoresDto>> GetPlayerLobbiesAsync(int userId, int page, int pageSize);
        Task<bool> DeleteLobbyAsync(int lobbyId);
    }
}
