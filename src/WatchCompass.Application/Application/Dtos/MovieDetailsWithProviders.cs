namespace WatchCompass.Application.Dtos;

public sealed record MovieDetailsWithProviders(int MovieId, string Title, int RuntimeMinutes, IReadOnlyList<string> Genres, IReadOnlyList<string> Providers);
