namespace WatchCompass.Application.Dtos;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalResults,
    int TotalPages,
    bool HasNextPage);
