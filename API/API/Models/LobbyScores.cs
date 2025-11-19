namespace API.Models
{
    public class LobbyScores
    {
        public string LobbyCode { get; set; } = string.Empty;
        public List<PlayerScore<User, RoundScore>> PlayerScores { get; set; } = new();

        public PlayerScore<User, RoundScore> GetOrCreatePlayerScore(int userId, string? playerName = null)
        {
            var playerScore = PlayerScores.FirstOrDefault(ps => ps.UserId == userId);

            if (playerScore == null)
            {
                playerScore = new PlayerScore<User, RoundScore>
                {
                    Player = new User
                    {
                        Id = userId,
                        Name = playerName
                    }
                   
                };
                PlayerScores.Add(playerScore);
            }
            else if (!string.IsNullOrEmpty(playerName) && playerScore.Player != null)
            {
                playerScore.Player.Name = playerName;
            }

            return playerScore;
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
            return PlayerScores
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