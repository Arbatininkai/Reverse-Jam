using System.Collections.Concurrent;

namespace API.Models
{
    public class PlayerScore<TUser, TRoundScore>
        where TUser : User
        where TRoundScore : RoundScore, new()
    {
        public TUser? Player { get; set; }

        public int UserId => Player?.Id ?? 0;
        public string? PlayerName => Player?.Name;

        private int _totalScore;
        public int TotalScore => _totalScore;

        public ConcurrentDictionary<int, RoundScore> RoundScores { get; set; }
            = new ConcurrentDictionary<int, RoundScore>();

        public RoundScore AddScore(int round, int score)
        {
            var roundScore = RoundScores.AddOrUpdate(
                round,
                _ => new RoundScore { RoundNumber = round, Score = score },
                (_, existing) =>
                {
                    existing.Score += score;
                    return existing;
                }
            );

            Interlocked.Add(ref _totalScore, score);
            return roundScore;
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