using Services.AuthService;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("google-signin")]
    public async Task<IActionResult> GoogleSignIn([FromBody] string idToken)
    {
        try
        {
            var authResult = await _authService.GoogleSignInAsync(idToken);
            var user = authResult.User;
            var token = authResult.Token;
            return Ok(new { user, token });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Invalid Google token: " + ex.Message });
        }
    }

    [Authorize]
    [HttpGet("userinfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email)) return Unauthorized(new { message = "User email missing in token" });

        var user = await _authService.GetUserByEmailAsync(email);
        if (user == null) return NotFound(new { message = "User not found" });

        return Ok(user);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        return Ok(new { userId, email });
    }
}
