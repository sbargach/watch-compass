import { useEffect, useRef } from "react";
import { MovieGrid } from "./MovieGrid";
import type { MovieCard, MovieDetails } from "../types/movies";
import { PosterArtwork } from "./PosterArtwork";
import { formatMovieMeta } from "../utils/movieFormatting";

type MovieDetailsPanelProps = {
  selectedMovie: MovieCard;
  details: MovieDetails | null;
  similarMovies: MovieCard[];
  isLoading: boolean;
  isSimilarLoading: boolean;
  error: string | null;
  similarError: string | null;
  watchRegionLabel: string;
  onClose: () => void;
  onRetry: () => void;
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
  watchRegionLabel,
  onClose,
  onRetry,
  onSelectMovie
}: MovieDetailsPanelProps) {
  const headingRef = useRef<HTMLHeadingElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const movie = details ?? selectedMovie;
  const hasProviders = details !== null && details.providers.length > 0;

  useEffect(() => {
    returnFocusRef.current = document.activeElement instanceof HTMLElement
      ? document.activeElement
      : null;
    return () => returnFocusRef.current?.focus();
  }, []);

  useEffect(() => {
    headingRef.current?.focus();
  }, [selectedMovie.movieId]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [onClose]);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, []);

  return (
    <div className="details-overlay" onClick={onClose}>
      <section
        className="details-panel"
        role="dialog"
        aria-modal="true"
        aria-live="polite"
        aria-labelledby="movie-details-heading"
        onClick={(event) => event.stopPropagation()}
      >
        <div
          className="details-image-backdrop"
          style={movie.backdropUrl ? { backgroundImage: `linear-gradient(180deg, rgba(12, 18, 16, 0.18), rgba(12, 18, 16, 0.9)), url(${movie.backdropUrl})` } : undefined}
        />

        <div className="details-card">
          <div className="details-header">
            <p className="details-eyebrow">Selected movie</p>
            <button type="button" className="details-close" onClick={onClose} aria-label="Close details">
              Close
            </button>
          </div>

          <div className="details-scroll">
            <div className="details-hero">
              <div className="details-poster">
                <PosterArtwork title={movie.title} posterUrl={movie.posterUrl} />
              </div>

              <div className="details-copy">
                <h2 id="movie-details-heading" ref={headingRef} tabIndex={-1}>{movie.title}</h2>
                <p className="details-meta">{formatMovieMeta(movie)}</p>
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

                {isLoading && (
                  <p className="status-text">
                    {details ? `Refreshing availability for ${watchRegionLabel}...` : "Loading movie details..."}
                  </p>
                )}
                {error && (
                  <div className="recovery-action" role="alert">
                    <p className="status-text status-error">{error}</p>
                    <button type="button" className="secondary-button" onClick={onRetry}>Retry details</button>
                  </div>
                )}

                {!isLoading && !error && (
                  <div className="providers-block">
                    <p className="providers-label">Where to watch in {watchRegionLabel}</p>
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
              {similarError && (
                <div className="recovery-action" role="alert">
                  <p className="status-text status-error">{similarError}</p>
                  <button type="button" className="secondary-button" onClick={onRetry}>Retry similar titles</button>
                </div>
              )}
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
        </div>
      </section>
    </div>
  );
}
