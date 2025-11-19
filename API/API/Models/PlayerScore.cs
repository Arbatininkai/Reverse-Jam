using System.Collections.Concurrent;

namespace API.Models
{
    public class PlayerScore
    {
        public int UserId { get; set; }
        public string? PlayerName { get; set; }
        private int _totalScore;
        public int TotalScore => _totalScore;
        public ConcurrentDictionary<int, RoundScore> RoundScores { get; set; }
            = new ConcurrentDictionary<int, RoundScore>();

        public void AddScore(int round, int score)
        {
            RoundScores.AddOrUpdate(
                round,
                _ => new RoundScore { RoundNumber = round, Score = score },
                (_, existing) =>
                {
                    existing.Score += score;
                    return existing;
                }
            );

            Interlocked.Add(ref _totalScore, score);
        }

        public int GetRoundScore(int round)
        {
            return RoundScores.TryGetValue(round, out var rs) ? rs.Score : 0;
        }
    }

    public class RoundScore
    {
        public int RoundNumber { get; set; }
        public int Score { get; set; }
    }
}