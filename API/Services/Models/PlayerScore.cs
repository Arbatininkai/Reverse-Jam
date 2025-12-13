using System.Collections.Concurrent;
using System.Threading;

namespace Services.Models
{
    public class PlayerScore<TUser>
        where TUser : class
    {
        public TUser? Player { get; set; }
        public int UserId { get; set; }
        public string? PlayerName { get; set; }

        private int _totalScore;
        public int TotalScore => _totalScore;

        public ConcurrentDictionary<int, RoundScore> RoundScores { get; } = new();

        public void AddScore(int round, int score)
        {
            RoundScores.AddOrUpdate(
                round,
                _ => new RoundScore { RoundNumber = round, Score = score },
                (_, existing) =>
                {
                    existing.Score += score;
                    return existing;
                });

            Interlocked.Add(ref _totalScore, score);
        }
    }


    public class RoundScore
    {
        public int RoundNumber { get; set; }
        public int Score { get; set; }
    }
}
