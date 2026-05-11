using Microsoft.AspNetCore.Mvc;
using PathfinderAI.Contracts.Common;
using PathfinderAI.Contracts.LearningPlans;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/learning-plans")]
public sealed class LearningPlansController : ControllerBase
{
    [HttpPost]
    public ActionResult<GenerateLearningPlanResponse> GenerateLearningPlan([FromBody] GenerateLearningPlanRequest request)
    {
        var response = new GenerateLearningPlanResponse(
            LearningPlanId: Guid.NewGuid(),
            Status: PlanGenerationStatus.Pending,
            RequestedAtUtc: DateTimeOffset.UtcNow);

        return Accepted(response);
    }

    [HttpGet("active")]
    public ActionResult<GetLearningPlanResponse> GetActiveLearningPlan()
    {
        var plan = new LearningPlanContract(
            LearningPlanId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            TrackId: "backend",
            TrackDisplayName: "Backend Development",
            ExperienceLevel: ExperienceLevel.Beginner,
            PreferredLocale: "en-US",
            Status: LearningPlanStatus.Active,
            EstimatedTotalHours: 120,
            WeeklyStudyHours: 5,
            CreatedOn: DateOnly.FromDateTime(DateTime.UtcNow),
            TargetCompletionDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(24 * 7)),
            Modules: Array.Empty<LearningModuleContract>());

        return Ok(new GetLearningPlanResponse(plan));
    }

    [HttpPost("{learningPlanId:guid}/regenerate")]
    public ActionResult<RegenerateLearningPlanResponse> RegenerateLearningPlan(
        Guid learningPlanId,
        [FromBody] RegenerateLearningPlanRequest request)
    {
        var response = new RegenerateLearningPlanResponse(
            LearningPlanId: learningPlanId,
            Status: PlanGenerationStatus.Pending,
            RequestedAtUtc: DateTimeOffset.UtcNow);

        return Accepted(response);
    }
}
