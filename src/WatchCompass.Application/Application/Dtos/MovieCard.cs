namespace WatchCompass.Application.Dtos;

public sealed record MovieCard(int MovieId, string Title, int? RuntimeMinutes, IReadOnlyList<string> Genres);
