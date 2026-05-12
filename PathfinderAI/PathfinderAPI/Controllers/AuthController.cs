using Microsoft.AspNetCore.Mvc;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required" });
        }

        var isEditor =
            request.Email.Contains("editor", StringComparison.OrdinalIgnoreCase) ||
            request.Email.Contains("admin", StringComparison.OrdinalIgnoreCase);

        var user = new DemoUserResponse
        {
            Id = "user-" + Math.Abs(request.Email.GetHashCode()),
            Name = request.Email.Split("@")[0],
            Email = request.Email,
            Role = isEditor ? "editor" : "user",
            HasCompletedOnboarding = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Preferences = new UserPreferences
            {
                Notifications = true,
                EmailUpdates = true
            }
        };

        var token = "demo-token-" + user.Id;

        return Ok(new LoginResponse
        {
            Token = token,
            User = user
        });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var authHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new { message = "Missing token" });
        }

        return Ok(new
        {
            message = "Token accepted",
            token = authHeader.Replace("Bearer ", "")
        });
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string Token { get; set; } = "";
    public DemoUserResponse User { get; set; } = new();
}

public class DemoUserResponse
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "user";
    public bool HasCompletedOnboarding { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserPreferences Preferences { get; set; } = new();
}

public class UserPreferences
{
    public bool Notifications { get; set; }
    public bool EmailUpdates { get; set; }
}