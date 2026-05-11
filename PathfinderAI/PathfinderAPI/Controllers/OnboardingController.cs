using Microsoft.AspNetCore.Mvc;
using PathfinderAI.Contracts.Common;
using PathfinderAI.Contracts.Onboarding;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/onboarding")]
public sealed class OnboardingController : ControllerBase
{
    [HttpGet("tracks")]
    public ActionResult<GetCareerTracksResponse> GetCareerTracks()
    {
        var tracks = new List<CareerTrackSummaryContract>
        {
            new("backend", "Backend Development", "Build scalable server-side applications and APIs.",
                new[] { new SkillTagContract("cs", "C#"), new SkillTagContract("sql", "SQL") },
                IsFeatured: true),
            new("frontend", "Frontend Development", "Create modern, responsive web interfaces.",
                new[] { new SkillTagContract("ts", "TypeScript"), new SkillTagContract("react", "React") },
                IsFeatured: true)
        };

        return Ok(new GetCareerTracksResponse(tracks));
    }

    [HttpGet("status")]
    public ActionResult<OnboardingStatusResponse> GetOnboardingStatus()
    {
        var response = new OnboardingStatusResponse(
            IsCompleted: false,
            ActiveLearningPlanId: null,
            CompletedAtUtc: null,
            Snapshot: null);

        return Ok(response);
    }

    [HttpPost("complete")]
    public ActionResult<CompleteOnboardingResponse> CompleteOnboarding([FromBody] CompleteOnboardingRequest request)
    {
        var response = new CompleteOnboardingResponse(
            UserId: Guid.NewGuid(),
            LearningPlanId: Guid.NewGuid(),
            GenerationStatus: PlanGenerationStatus.Pending,
            RequestedAtUtc: DateTimeOffset.UtcNow);

        return Accepted(response);
    }
}
