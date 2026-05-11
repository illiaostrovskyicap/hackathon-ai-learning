using PathfinderAI.Contracts.Common;

namespace PathfinderAI.Contracts.Accounts;

public sealed record UserPreferencesContract(
    string PreferredLocale,
    ExperienceLevel? ExperienceLevel,
    int WeeklyStudyHours,
    bool AllowPersonalizedRecommendations,
    bool AllowAnalyticsTracking);

public sealed record UserProfileContract(
    Guid UserId,
    string ExternalIdentityId,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    UserPreferencesContract Preferences,
    bool HasActiveLearningPlan,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record GetCurrentUserResponse(UserProfileContract User);

public sealed record UpdateUserProfileRequest(
    string DisplayName,
    string? AvatarUrl,
    string PreferredLocale,
    ExperienceLevel? ExperienceLevel,
    int WeeklyStudyHours,
    bool AllowPersonalizedRecommendations,
    bool AllowAnalyticsTracking);

public sealed record DeleteAccountRequest(
    string? Reason,
    bool DeletePersonalData);

public sealed record DeleteAccountResponse(
    Guid UserId,
    string RequestStatus,
    DateTimeOffset RequestedAtUtc);
