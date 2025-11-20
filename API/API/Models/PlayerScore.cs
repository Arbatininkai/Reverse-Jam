using System.Collections.Concurrent;

namespace API.Models
{
    public class PlayerScore<TUser, TRoundScore>
        where TUser : User
        where TRoundScore : RoundScore, new()
    {
//i don't now how it need to be here
/*
        public int UserId { get; set; }
        public string? PlayerName { get; set; }
        private int _totalScore;
        public int TotalScore => _totalScore;
*/


        public TUser? Player { get; set; }

        public int UserId => Player?.Id ?? 0;
        public string? PlayerName => Player?.Name;

        public int TotalScore { get; private set; }

        public List<TRoundScore> RoundScores { get; } = new();
        public ConcurrentDictionary<int, RoundScore> RoundScores { get; set; }
            = new ConcurrentDictionary<int, RoundScore>();
      
        public TRoundScore AddScore(int round, int score)
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