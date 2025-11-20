using System.Collections.Concurrent;

namespace API.Models
{
    public class LobbyScores
    {
        public string LobbyCode { get; set; } = string.Empty;
        public ConcurrentDictionary<int, PlayerScore<User, RoundScore>> PlayerScores { get; set; }
                    = new ConcurrentDictionary<int, PlayerScore<User, RoundScore>>();

        public PlayerScore<User, RoundScore> GetOrCreatePlayerScore(int userId, string? playerName = null)
        {
            return PlayerScores.AddOrUpdate(
               userId,
               _ => new PlayerScore<User, RoundScore> { Player = new User { Id = userId, Name = playerName } },
               (_, existing) =>
               {
                   if (!string.IsNullOrEmpty(playerName) && existing.Player != null)
                       existing.Player.Name = playerName;
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