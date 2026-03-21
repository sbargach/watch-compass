import { MovieGrid } from "./MovieGrid";
import type { MovieCard, MovieDetails } from "../types/movies";

type MovieDetailsPanelProps = {
  selectedMovie: MovieCard;
  details: MovieDetails | null;
  similarMovies: MovieCard[];
  isLoading: boolean;
  isSimilarLoading: boolean;
  error: string | null;
  similarError: string | null;
  onClose: () => void;
  onSelectMovie: (movie: MovieCard) => void;
};

export function MovieDetailsPanel({
  selectedMovie,
  details,
  similarMovies,
  isLoading,
  isSimilarLoading,
  error,
  similarError,
  onClose,
  onSelectMovie
}: MovieDetailsPanelProps) {
  const movie = details ?? selectedMovie;
  const meta = buildMeta(movie);
  const hasProviders = details !== null && details.providers.length > 0;

  return (
    <section className="details-panel" aria-live="polite">
      <div
        className="details-backdrop"
        style={movie.backdropUrl ? { backgroundImage: `linear-gradient(180deg, rgba(19, 32, 27, 0.28), rgba(19, 32, 27, 0.9)), url(${movie.backdropUrl})` } : undefined}
      />

      <div className="details-card">
        <div className="details-header">
          <p className="details-eyebrow">Selected movie</p>
          <button type="button" className="details-close" onClick={onClose}>
            Close
          </button>
        </div>

        <div className="details-hero">
          <div className="details-poster">
            {movie.posterUrl ? (
              <img src={movie.posterUrl} alt={`${movie.title} poster`} loading="lazy" />
            ) : (
              <div className="poster-fallback" aria-hidden="true">
                {movie.title.slice(0, 1).toUpperCase()}
              </div>
            )}
          </div>

          <div className="details-copy">
            <h2>{movie.title}</h2>
            <p className="details-meta">{meta}</p>
            <p className="details-overview">{movie.overview?.trim() || "No overview available yet."}</p>

            {movie.genres.length > 0 && (
              <div className="chip-list" aria-label="Genres">
                {movie.genres.map((genre) => (
                  <span key={genre} className="chip">
                    {genre}
                  </span>
                ))}
              </div>
            )}

            {isLoading && <p className="status-text">Loading movie details...</p>}
            {error && <p className="status-text status-error">{error}</p>}

            {!isLoading && !error && (
              <div className="providers-block">
                <p className="providers-label">Where to watch</p>
                {hasProviders ? (
                  <div className="chip-list" aria-label="Providers">
                    {details.providers.map((provider) => (
                      <span key={provider} className="chip chip-provider">
                        {provider}
                      </span>
                    ))}
                  </div>
                ) : (
                  <p className="status-text">No provider data was returned for this title.</p>
                )}
              </div>
            )}
          </div>
        </div>

        <div className="details-section">
          <div className="section-heading">
            <h3>Similar titles</h3>
            <p>Use the panel to keep exploring without losing your current results.</p>
          </div>

          {isSimilarLoading && <p className="status-text">Loading similar movies...</p>}
          {similarError && <p className="status-text status-error">{similarError}</p>}
          {!isSimilarLoading && !similarError && similarMovies.length === 0 && (
            <p className="status-text">No similar titles were returned.</p>
          )}
          {similarMovies.length > 0 && (
            <MovieGrid
              movies={similarMovies}
              onSelectMovie={onSelectMovie}
              selectedMovieId={selectedMovie.movieId}
            />
          )}
        </div>
      </div>
    </section>
  );
}

function buildMeta(movie: MovieCard): string {
  const meta: string[] = [];

  if (movie.releaseYear) {
    meta.push(String(movie.releaseYear));
  }

  if (movie.runtimeMinutes) {
    meta.push(`${movie.runtimeMinutes} min`);
  }

  return meta.length > 0 ? meta.join(" | ") : "Runtime unavailable";
}
