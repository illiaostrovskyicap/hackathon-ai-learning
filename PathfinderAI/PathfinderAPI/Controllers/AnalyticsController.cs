using Microsoft.AspNetCore.Mvc;
using PathfinderAI.Contracts.Analytics;
using PathfinderAI.Contracts.Common;

namespace PathfinderAPI.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class AnalyticsController : ControllerBase
{
    [HttpGet("summary")]
    public ActionResult<GetDashboardSummaryResponse> GetDashboardSummary(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        var summary = new DashboardSummaryContract(
            Range: new DateRange(from, to),
            TotalModules: 0,
            CompletedModules: 0,
            InProgressModules: 0,
            TotalLearningMinutes: 0,
            CompletionRate: 0m,
            WeeklyActivity: Array.Empty<WeeklyActivityPointContract>(),
            Skills: Array.Empty<SkillProgressContract>());

        return Ok(new GetDashboardSummaryResponse(summary));
    }
}
