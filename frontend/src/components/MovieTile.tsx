import type { MovieCard } from "../types/movies";

type MovieTileProps = {
  movie: MovieCard;
};

export function MovieTile({ movie }: MovieTileProps) {
  const meta: string[] = [];
  if (movie.releaseYear) {
    meta.push(String(movie.releaseYear));
  }
  if (movie.runtimeMinutes) {
    meta.push(`${movie.runtimeMinutes} min`);
  }

  const genreText = movie.genres.length > 0 ? movie.genres.slice(0, 3).join(" / ") : "Genre unavailable";

  return (
    <article className="movie-tile">
      <div className="movie-tile-poster">
        {movie.posterUrl ? (
          <img src={movie.posterUrl} alt={`${movie.title} poster`} loading="lazy" />
        ) : (
          <div className="poster-fallback" aria-hidden="true">
            {movie.title.slice(0, 1).toUpperCase()}
          </div>
        )}
      </div>

      <div className="movie-tile-content">
        <h3>{movie.title}</h3>
        <p className="movie-meta">{meta.length > 0 ? meta.join(" | ") : "Runtime unavailable"}</p>
        <p className="movie-genres">{genreText}</p>
        <p className="movie-overview">{movie.overview?.trim() || "No overview available yet."}</p>
      </div>
    </article>
  );
}
