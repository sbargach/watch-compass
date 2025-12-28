namespace WatchCompass.Application.Dtos;

public sealed record Recommendation(int MovieId, string Title, int RuntimeMinutes, IReadOnlyList<string> Reasons, IReadOnlyList<string> Providers);
