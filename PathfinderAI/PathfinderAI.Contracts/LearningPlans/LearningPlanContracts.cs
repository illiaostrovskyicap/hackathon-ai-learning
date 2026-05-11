using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.LearningPlans;

public sealed record LearningResourceContract(
    string ResourceId,
    string Title,
    LearningResourceType ResourceType,
    string Url,
    string Provider,
    bool IsRequired);

public sealed record LearningModuleContract(
    Guid ModuleId,
    int Order,
    string Title,
    string Description,
    int EstimatedHours,
    LearningModuleStatus Status,
    IReadOnlyCollection<SkillTagContract> Skills,
    IReadOnlyCollection<LearningResourceContract> Resources);

public sealed record LearningPlanContract(
    Guid LearningPlanId,
    Guid UserId,
    string TrackId,
    string TrackDisplayName,
    ExperienceLevel ExperienceLevel,
    string PreferredLocale,
    LearningPlanStatus Status,
    int EstimatedTotalHours,
    int WeeklyStudyHours,
    DateOnly CreatedOn,
    DateOnly? TargetCompletionDate,
    IReadOnlyCollection<LearningModuleContract> Modules);

public sealed record GenerateLearningPlanRequest(
    string TrackId,
    ExperienceLevel ExperienceLevel,
    string PreferredLocale,
    int WeeklyStudyHours,
    int TargetCompletionWeeks,
    bool ForceRegeneration = false);

public sealed record GenerateLearningPlanResponse(
    Guid LearningPlanId,
    PlanGenerationStatus Status,
    DateTimeOffset RequestedAtUtc);

public sealed record GetLearningPlanResponse(LearningPlanContract Plan);

public sealed record RegenerateLearningPlanRequest(
    int? WeeklyStudyHours = null,
    int? TargetCompletionWeeks = null,
    string? Reason = null);

public sealed record RegenerateLearningPlanResponse(
    Guid LearningPlanId,
    PlanGenerationStatus Status,
    DateTimeOffset RequestedAtUtc);
