using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required" });

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            return StatusCode(500, new { message = "Database connection string is missing" });

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await EnsureUsersTable(connection);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        await using var findCommand = new NpgsqlCommand(
            """
            SELECT id, email, name, password_hash, role, has_completed_onboarding, created_at
            FROM users
            WHERE email = @email
            """,
            connection
        );

        findCommand.Parameters.AddWithValue("email", normalizedEmail);

        await using var reader = await findCommand.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var userId = reader.GetGuid(0);
            var email = reader.GetString(1);
            var name = reader.GetString(2);
            var passwordHash = reader.GetString(3);
            var role = reader.GetString(4);
            var hasCompletedOnboarding = reader.GetBoolean(5);
            var createdAt = reader.GetDateTime(6);

            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(new LoginResponse
            {
                Token = "demo-token-" + userId,
                User = new UserResponse
                {
                    Id = userId,
                    Email = email,
                    Name = name,
                    Role = role,
                    HasCompletedOnboarding = hasCompletedOnboarding,
                    CreatedAt = createdAt
                }
            });
        }

        await reader.CloseAsync();

        var newUserId = Guid.NewGuid();
        var newName = normalizedEmail.Split("@")[0];
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var newRole = normalizedEmail.Contains("admin") || normalizedEmail.Contains("editor")
            ? "editor"
            : "user";

        await using var insertCommand = new NpgsqlCommand(
            """
            INSERT INTO users (id, email, name, password_hash, role, has_completed_onboarding, created_at)
            VALUES (@id, @email, @name, @passwordHash, @role, false, NOW())
            """,
            connection
        );

        insertCommand.Parameters.AddWithValue("id", newUserId);
        insertCommand.Parameters.AddWithValue("email", normalizedEmail);
        insertCommand.Parameters.AddWithValue("name", newName);
        insertCommand.Parameters.AddWithValue("passwordHash", newPasswordHash);
        insertCommand.Parameters.AddWithValue("role", newRole);

        await insertCommand.ExecuteNonQueryAsync();

        return Ok(new LoginResponse
        {
            Token = "demo-token-" + newUserId,
            User = new UserResponse
            {
                Id = newUserId,
                Email = normalizedEmail,
                Name = newName,
                Role = newRole,
                HasCompletedOnboarding = false,
                CreatedAt = DateTime.UtcNow
            }
        });
    }

    private static async Task EnsureUsersTable(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                email TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'user',
                has_completed_onboarding BOOLEAN NOT NULL DEFAULT false,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );
            """,
            connection
        );

        await command.ExecuteNonQueryAsync();
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
    public UserResponse User { get; set; } = new();
}

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "user";
    public bool HasCompletedOnboarding { get; set; }
    public DateTime CreatedAt { get; set; }
}