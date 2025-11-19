using System.Collections.Concurrent;

namespace API.Models
{
    public class LobbyScores
    {
        public string LobbyCode { get; set; } = string.Empty;
        public ConcurrentDictionary<int, PlayerScore> PlayerScores { get; set; }
                    = new ConcurrentDictionary<int, PlayerScore>();

        public PlayerScore GetOrCreatePlayerScore(int userId, string? playerName = null)
        {
            return PlayerScores.AddOrUpdate(
               userId,
               _ => new PlayerScore { UserId = userId, PlayerName = playerName },
               (_, existing) =>
               {
                   if (!string.IsNullOrEmpty(playerName))
                       existing.PlayerName = playerName;
                   return existing;
               }
           );
        }

        public void AddVotes(List<VoteDto> votes, int round)
        {
            foreach (var vote in votes)
            {
                var playerScore = GetOrCreatePlayerScore(vote.TargetUserId);
                playerScore.AddScore(round, vote.Score);
            }
        }

        public List<ScoreEntry> GetFinalScores()
        {
            return PlayerScores.Values
                .Select(ps => new ScoreEntry
                {
                    UserId = ps.UserId,
                    Name = ps.PlayerName,
                    Score = ps.TotalScore
                })
                .OrderByDescending(se => se.Score)
                .ThenBy(se => se.Name)
                .ToList();
        }
    }
}