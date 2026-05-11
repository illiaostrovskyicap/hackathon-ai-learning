using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.Assistant;

public sealed record LearningAssistantMessageContract(
    Guid MessageId,
    AssistantMessageRole Role,
    string Content,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<SourceCitationContract> Citations);

public sealed record LearningAssistantSessionContract(
    Guid SessionId,
    Guid UserId,
    Guid? LearningPlanId,
    string? TrackId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<LearningAssistantMessageContract> Messages);

public sealed record StartLearningAssistantSessionRequest(
    Guid? LearningPlanId,
    string? TrackId);

public sealed record StartLearningAssistantSessionResponse(
    Guid SessionId,
    DateTimeOffset CreatedAtUtc);

public sealed record AskLearningAssistantRequest(
    Guid? SessionId,
    Guid? LearningPlanId,
    string? TrackId,
    string Question);

public sealed record AskLearningAssistantResponse(
    Guid SessionId,
    LearningAssistantMessageContract Answer,
    bool UsedFallbackResponse);
