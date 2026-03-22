import type { MovieCard } from "../types/movies";
import { PosterArtwork } from "./PosterArtwork";

type MovieTileProps = {
  movie: MovieCard;
  onSelect?: (movie: MovieCard) => void;
  isActive?: boolean;
};

export function MovieTile({ movie, onSelect, isActive = false }: MovieTileProps) {
  const meta: string[] = [];
  if (movie.releaseYear) {
    meta.push(String(movie.releaseYear));
  }
  if (movie.runtimeMinutes) {
    meta.push(`${movie.runtimeMinutes} min`);
  }

  const genreText = movie.genres.length > 0 ? movie.genres.slice(0, 3).join(" / ") : "Genre unavailable";

  const content = (
    <>
      <div className="movie-tile-poster">
        <PosterArtwork title={movie.title} posterUrl={movie.posterUrl} />
      </div>

      <div className="movie-tile-content">
        <h3>{movie.title}</h3>
        <p className="movie-meta">{meta.length > 0 ? meta.join(" | ") : "Runtime unavailable"}</p>
        <p className="movie-genres">{genreText}</p>
        <p className="movie-overview">{movie.overview?.trim() || "No overview available yet."}</p>
      </div>
    </>
  );

  if (onSelect) {
    return (
      <article className={`movie-tile movie-tile-selectable${isActive ? " movie-tile-active" : ""}`}>
        <button
          type="button"
          className="movie-tile-button"
          onClick={() => onSelect(movie)}
          aria-label={`View details for ${movie.title}`}
          aria-pressed={isActive}
        />
        {content}
      </article>
    );
  }

  return <article className="movie-tile">{content}</article>;
}
