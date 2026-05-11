using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.Progress;

public sealed record ModuleProgressContract(
    Guid LearningPlanId,
    Guid ModuleId,
    LearningModuleStatus Status,
    int TotalMinutesSpent,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateModuleProgressRequest(
    LearningModuleStatus Status,
    int MinutesSpentDelta,
    DateTimeOffset OccurredAtUtc);

public sealed record UpdateModuleProgressResponse(
    ModuleProgressContract Progress);

public sealed record LearningActivityContract(
    Guid ActivityId,
    Guid LearningPlanId,
    Guid ModuleId,
    int MinutesSpent,
    DateOnly ActivityDate,
    DateTimeOffset LoggedAtUtc);

public sealed record LogLearningActivityRequest(
    Guid ModuleId,
    int MinutesSpent,
    DateOnly ActivityDate);

public sealed record LogLearningActivityResponse(
    LearningActivityContract Activity);
