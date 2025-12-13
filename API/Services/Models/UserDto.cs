namespace Services.Models;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string PhotoUrl { get; set; } = default!;
    public string Emoji { get; set; } = default!;
}
