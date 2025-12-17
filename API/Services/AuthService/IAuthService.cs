using Services.Models;

namespace Services.AuthService;

public interface IAuthService
{
    Task<AuthResponse> GoogleSignInAsync(string idToken);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<bool> UpdateUserProfileAsync(int userId, string? name, string? emoji);
}
