namespace WatchCompass.Application.Dtos;

public sealed record Recommendation(
    int MovieId,
    string Title,
    int RuntimeMinutes,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Providers,
    string? PosterUrl = null,
    string? BackdropUrl = null,
    int? ReleaseYear = null,
    string? Overview = null);
