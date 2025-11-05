using System.Collections.Generic;

namespace API.Models
{
    public class EndRoundRequest
    {
        public int? LobbyCode { get; set; } 
        public List<VoteDto> Votes { get; set; } = new();
    }

    public class VoteDto
    {
        public int TargetUserId { get; set; }
        public int Score { get; set; }
    }
}