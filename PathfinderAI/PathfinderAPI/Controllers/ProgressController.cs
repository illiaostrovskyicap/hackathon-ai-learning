using Microsoft.AspNetCore.Mvc;
using PathfinderAI.Contracts.Common;
using PathfinderAI.Contracts.Progress;

namespace PathfinderAPI.Controllers;

[ApiController]
public sealed class ProgressController : ControllerBase
{
    [HttpPatch("api/learning-plans/{learningPlanId:guid}/modules/{moduleId:guid}")]
    public ActionResult<UpdateModuleProgressResponse> UpdateModuleProgress(
        Guid learningPlanId,
        Guid moduleId,
        [FromBody] UpdateModuleProgressRequest request)
    {
        var progress = new ModuleProgressContract(
            LearningPlanId: learningPlanId,
            ModuleId: moduleId,
            Status: request.Status,
            TotalMinutesSpent: request.MinutesSpentDelta,
            StartedAtUtc: request.Status == LearningModuleStatus.NotStarted ? null : request.OccurredAtUtc,
            CompletedAtUtc: request.Status == LearningModuleStatus.Completed ? request.OccurredAtUtc : null,
            UpdatedAtUtc: DateTimeOffset.UtcNow);

        return Ok(new UpdateModuleProgressResponse(progress));
    }

    [HttpPost("api/progress/activities")]
    public ActionResult<LogLearningActivityResponse> LogLearningActivity(
        [FromBody] LogLearningActivityRequest request)
    {
        var activity = new LearningActivityContract(
            ActivityId: Guid.NewGuid(),
            LearningPlanId: Guid.NewGuid(),
            ModuleId: request.ModuleId,
            MinutesSpent: request.MinutesSpent,
            ActivityDate: request.ActivityDate,
            LoggedAtUtc: DateTimeOffset.UtcNow);

        return CreatedAtAction(nameof(LogLearningActivity), new LogLearningActivityResponse(activity));
    }
}
