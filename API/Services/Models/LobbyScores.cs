using System.Collections.Concurrent;

namespace Services.Models;

public class LobbyScores
{
    public string LobbyCode { get; set; } = string.Empty;

    public ConcurrentDictionary<int, PlayerScore<UserDto>> PlayerScores { get; }
        = new();

    public PlayerScore<UserDto> GetOrCreatePlayerScore(int userId, string name = "")
    {
        return PlayerScores.GetOrAdd(userId, _ =>
            new PlayerScore<UserDto>
            {
                UserId = userId,
                PlayerName = name,
                Player = new UserDto { Id = userId, Name = name }
            });
    }

    public void AddVotes(List<VoteDto> votes, int round)
    {
        foreach (var vote in votes)
        {
            var ps = GetOrCreatePlayerScore(vote.TargetUserId);
            ps.AddScore(round, vote.Score);
        }
    }

    public List<PlayerScore<UserDto>> GetPlayerScores()
    {
        return PlayerScores.Values
            .OrderByDescending(ps => ps.TotalScore)
            .ToList();
    }
}
