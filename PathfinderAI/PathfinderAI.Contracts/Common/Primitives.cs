namespace PathfinderAI.Contracts.Common;

public sealed record DateRange(DateOnly From, DateOnly To);

public sealed record PaginationRequest(int Page = 1, int PageSize = 20);

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record ProblemDetailsContract(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record SourceCitationContract(
    string Title,
    string Url,
    string SourceType,
    string? Snippet = null);

public sealed record SkillTagContract(
    string SkillId,
    string DisplayName);
