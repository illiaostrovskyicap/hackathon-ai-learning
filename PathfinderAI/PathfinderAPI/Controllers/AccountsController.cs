using Microsoft.AspNetCore.Mvc;
using PathfinderAI.Contracts.Accounts;
using PathfinderAI.Contracts.Common;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/me")]
public sealed class AccountsController : ControllerBase
{
    [HttpGet]
    public ActionResult<GetCurrentUserResponse> GetCurrentUser()
    {
        var user = new UserProfileContract(
            Guid.NewGuid(),
            "external-id",
            "user@example.com",
            "Example User",
            null,
            new UserPreferencesContract("en-US", ExperienceLevel.Beginner, 3, false, false),
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        return Ok(new GetCurrentUserResponse(user));
    }

    [HttpPatch]
    public ActionResult<GetCurrentUserResponse> UpdateCurrentUser([FromBody] UpdateUserProfileRequest request)
    {
        var user = new UserProfileContract(
            Guid.NewGuid(),
            "external-id",
            "user@example.com",
            request.DisplayName,
            request.AvatarUrl,
            new UserPreferencesContract(request.PreferredLocale, request.ExperienceLevel, request.WeeklyStudyHours, request.AllowPersonalizedRecommendations, request.AllowAnalyticsTracking),
            false,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

        return Ok(new GetCurrentUserResponse(user));
    }

    [HttpDelete]
    public ActionResult<DeleteAccountResponse> DeleteCurrentUser([FromBody] DeleteAccountRequest request)
    {
        var response = new DeleteAccountResponse(Guid.NewGuid(), "Accepted", DateTimeOffset.UtcNow);
        return Accepted(response);
    }
}
