import type { MovieCard } from "../types/movies";

export function formatMovieMeta(movie: Pick<MovieCard, "releaseYear" | "runtimeMinutes">): string {
  const meta: string[] = [];

  if (movie.releaseYear) {
    meta.push(String(movie.releaseYear));
  }

  if (movie.runtimeMinutes) {
    meta.push(`${movie.runtimeMinutes} min`);
  }

  return meta.length > 0 ? meta.join(" | ") : "Runtime unavailable";
}
