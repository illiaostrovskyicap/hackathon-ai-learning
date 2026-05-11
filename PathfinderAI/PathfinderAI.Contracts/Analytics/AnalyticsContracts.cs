using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.Analytics;

public sealed record WeeklyActivityPointContract(
    DateOnly WeekStartDate,
    int MinutesSpent,
    int ModulesCompleted);

public sealed record SkillProgressContract(
    string SkillId,
    string DisplayName,
    int CompletedModules,
    int TotalModules);

public sealed record DashboardSummaryContract(
    DateRange Range,
    int TotalModules,
    int CompletedModules,
    int InProgressModules,
    int TotalLearningMinutes,
    decimal CompletionRate,
    IReadOnlyCollection<WeeklyActivityPointContract> WeeklyActivity,
    IReadOnlyCollection<SkillProgressContract> Skills);

public sealed record GetDashboardSummaryResponse(
    DashboardSummaryContract Summary);
