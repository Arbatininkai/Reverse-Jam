using Services.GameService.Models;
using Microsoft.AspNetCore.Mvc;
public interface IGameService
{
    Task SubmitVotesAsync(EndRoundRequest request);
    Task<FinalScoreResponse> CalculateFinalScoresAsync(string lobbyCode, int userId);
}
