using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; } //gali but null
        public string? Name { get; set; }
        public string? PhotoUrl { get; set; }
        public int TotalWins { get; set; } = 0;
    }
}
