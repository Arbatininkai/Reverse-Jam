namespace API.Models
{
    public class PlayerScore
    {
        public int UserId { get; set; }
        public string? PlayerName { get; set; }
        public int TotalScore { get; set; }
        public List<RoundScore> RoundScores { get; set; } = new();

        public void AddScore(int round, int score)
        {
            
            var roundScore = RoundScores.FirstOrDefault(rs => rs.RoundNumber == round);
            if (roundScore != null)
            {
                roundScore.Score += score;
            }
            else
            {
                RoundScores.Add(new RoundScore { RoundNumber = round, Score = score });
            }

            TotalScore += score;
        }

        public int GetRoundScore(int round)
        {
            return RoundScores.FirstOrDefault(rs => rs.RoundNumber == round)?.Score ?? 0;
        }
    }

    public class RoundScore
    {
        public int RoundNumber { get; set; }
        public int Score { get; set; }
    }
}