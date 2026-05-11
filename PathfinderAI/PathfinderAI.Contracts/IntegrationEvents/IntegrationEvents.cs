using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }

    string EventName { get; }

    string Version { get; }
}

public sealed record UserOnboardingCompletedEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid UserId,
    string TrackId,
    ExperienceLevel ExperienceLevel,
    Guid LearningPlanId) : IIntegrationEvent
{
    public string EventName => "user.onboarding.completed";

    public string Version => "1.0";
}

public sealed record LearningPlanGenerationRequestedEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid UserId,
    Guid LearningPlanId,
    string TrackId,
    ExperienceLevel ExperienceLevel) : IIntegrationEvent
{
    public string EventName => "learning-plan.generation.requested";

    public string Version => "1.0";
}

public sealed record LearningPlanGeneratedEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid UserId,
    Guid LearningPlanId,
    PlanGenerationStatus Status,
    int ModuleCount) : IIntegrationEvent
{
    public string EventName => "learning-plan.generated";

    public string Version => "1.0";
}

public sealed record ModuleProgressUpdatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid UserId,
    Guid LearningPlanId,
    Guid ModuleId,
    LearningModuleStatus Status,
    int TotalMinutesSpent) : IIntegrationEvent
{
    public string EventName => "module.progress.updated";

    public string Version => "1.0";
}

public sealed record LearningAssistantAnsweredEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid UserId,
    Guid SessionId,
    bool UsedFallbackResponse,
    int CitationCount) : IIntegrationEvent
{
    public string EventName => "assistant.answered";

    public string Version => "1.0";
}
