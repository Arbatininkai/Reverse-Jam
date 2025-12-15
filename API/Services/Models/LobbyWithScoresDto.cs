using Services.Models;

public class LobbyWithScoresDto
{
    public LobbyDto Lobby { get; set; } = default!;
    public LobbyScores? Scores { get; set; }
}