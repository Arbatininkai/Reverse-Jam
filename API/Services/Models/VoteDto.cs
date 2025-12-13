namespace Services.Models;

public class VoteDto
{
    public int UserId { get; set; }
    public int Score { get; set; }
    public int TargetUserId { get => UserId; set => UserId = value; }
}
