
using Integrations.Data.Entities;
using Services.Models;
using System.Collections.Concurrent;

namespace Services.GameService.Models;

public class EndRoundRequest
{
    public string LobbyCode { get; set; } = string.Empty;
    public int Round { get; set; }
    public List<VoteDto> Votes { get; set; } = new();
}

public class FinalScoreResponse
{
    public List<PlayerScore<UserEntity>> Scores { get; set; } = new();
    public UserEntity? Winner { get; set; }
}

public class CalculateFinalScoresResponse
{
    public List<PlayerScoreDto> Scores { get; set; } = new();
    public UserDto Winner { get; set; } = null!;
}

public class PlayerScoreDto
{
    public int UserId { get; set; }
    public string PlayerName { get; set; } = "";
    public int TotalScore { get; set; }
    public Dictionary<int, RoundScoreDto> RoundScores { get; set; } = new();
}

public class RoundScoreDto
{
    public int RoundNumber { get; set; }
    public int Score { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

