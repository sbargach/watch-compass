namespace WatchCompass.Application.Dtos;

public sealed record MovieDetails(
    int MovieId,
    string Title,
    int RuntimeMinutes,
    IReadOnlyList<string> Genres,
    string? PosterUrl = null,
    string? BackdropUrl = null,
    int? ReleaseYear = null,
    string? Overview = null);
