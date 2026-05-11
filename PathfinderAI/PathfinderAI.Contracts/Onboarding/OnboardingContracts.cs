using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.Onboarding;

public sealed record CareerTrackSummaryContract(
    string TrackId,
    string DisplayName,
    string Description,
    IReadOnlyCollection<SkillTagContract> CoreSkills,
    bool IsFeatured);

public sealed record GetCareerTracksResponse(
    IReadOnlyCollection<CareerTrackSummaryContract> Tracks);

public sealed record UserGoalContract(
    string GoalId,
    string DisplayName);

public sealed record OnboardingSnapshotContract(
    string TrackId,
    ExperienceLevel ExperienceLevel,
    string PreferredLocale,
    int WeeklyStudyHours,
    int TargetCompletionWeeks,
    IReadOnlyCollection<string> GoalIds);

public sealed record OnboardingStatusResponse(
    bool IsCompleted,
    Guid? ActiveLearningPlanId,
    DateTimeOffset? CompletedAtUtc,
    OnboardingSnapshotContract? Snapshot);

public sealed record CompleteOnboardingRequest(
    string TrackId,
    ExperienceLevel ExperienceLevel,
    string PreferredLocale,
    int WeeklyStudyHours,
    int TargetCompletionWeeks,
    IReadOnlyCollection<string> GoalIds);

public sealed record CompleteOnboardingResponse(
    Guid UserId,
    Guid LearningPlanId,
    PlanGenerationStatus GenerationStatus,
    DateTimeOffset RequestedAtUtc);
