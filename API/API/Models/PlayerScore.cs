namespace API.Models
{
    public class PlayerScore<TUser, TRoundScore>
        where TUser : User
        where TRoundScore : RoundScore, new()
    {

        public TUser? Player { get; set; }

        public int UserId => Player?.Id ?? 0;
        public string? PlayerName => Player?.Name;

        public int TotalScore { get; private set; }

        public List<TRoundScore> RoundScores { get; } = new();

      
        public TRoundScore AddScore(int round, int score)
        {
            var roundScore = RoundScores.FirstOrDefault(rs => rs.RoundNumber == round);
            if (roundScore != null)
            {
                roundScore.Score += score;
            }
            else
            {
                roundScore = new TRoundScore
                {
                    RoundNumber = round,
                    Score = score
                };
                RoundScores.Add(roundScore);
            }

            TotalScore += score;
            return roundScore;
        }

        public int GetRoundScore(int round)
            => RoundScores.FirstOrDefault(rs => rs.RoundNumber == round)?.Score ?? 0;

     
    }

    
    public class RoundScore
    {
        public int RoundNumber { get; set; }
        public int Score { get; set; }
    }
}