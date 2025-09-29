using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; } //gali but null

        [Required]
        [MinLength(6)]
        public string? Password { get; set; }
        public string? Name { get; set; }
        public string? PholoUrl { get; set; }
    }
}
