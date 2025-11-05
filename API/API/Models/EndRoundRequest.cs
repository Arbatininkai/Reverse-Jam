using System.Collections.Generic;

namespace API.Models
{
    public class EndRoundRequest
    {
        public string LobbyCode { get; set; } = string.Empty;
        public List<VoteDto> Votes { get; set; } = new();
    }

    public class VoteDto
    {
        public int TargetUserId { get; set; }
        public int Score { get; set; }
    }
}