namespace Services.Models;

public class AuthResponse
{
    public UserDto User { get; set; } = default!;
    public string Token { get; set; } = default!;
}
