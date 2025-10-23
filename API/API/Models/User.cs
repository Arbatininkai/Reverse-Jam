using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class User
    {
        public User(int id, string email, string name, string photoUrl = "https://www.vhv.rs/viewpic/hTTJxbm_emoticon-logo-png-smiley-face-emoji-png-transparent/")
        {
            this.Id = id;
            this.Email = email;
            this.Name = name;
            this.photoUrl = photoUrl;
        }
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; } //gali but null
        public string? Name { get; set; }
        public string? photoUrl { get; set; }
    }
}
