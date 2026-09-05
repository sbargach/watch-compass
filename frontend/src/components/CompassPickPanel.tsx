import { useEffect, useRef, useState } from "react";
import type { MovieCard } from "../types/movies";
import { PosterArtwork } from "./PosterArtwork";
import { formatMovieMeta } from "../utils/movieFormatting";

type CompassPickPanelProps = {
  movies: MovieCard[];
  selectedMovieId?: number | null;
  isCatalogLoading: boolean;
  hasCatalogError: boolean;
  onSelectMovie: (movie: MovieCard) => void;
};

const SPIN_DURATION_MS = 950;
const BASE_SPIN_DEGREES = 1080;

export function CompassPickPanel({
  movies,
  selectedMovieId = null,
  isCatalogLoading,
  hasCatalogError,
  onSelectMovie
}: CompassPickPanelProps) {
  const [pickedMovie, setPickedMovie] = useState<MovieCard | null>(null);
  const [isSpinning, setIsSpinning] = useState(false);
  const [compassRotation, setCompassRotation] = useState(26);
  const spinTimeoutRef = useRef<number | null>(null);
  const canSpin = movies.length > 0 && !isSpinning;

  useEffect(() => {
    return () => {
      if (spinTimeoutRef.current !== null) {
        window.clearTimeout(spinTimeoutRef.current);
      }
    };
  }, []);

  const handleSpin = () => {
    if (movies.length === 0) {
      return;
    }

    const movie = movies[Math.floor(Math.random() * movies.length)];
    const extraRotation = Math.round(Math.random() * 360);

    if (spinTimeoutRef.current !== null) {
      window.clearTimeout(spinTimeoutRef.current);
    }

    setIsSpinning(true);
    setPickedMovie(null);
    setCompassRotation((currentRotation) => currentRotation + BASE_SPIN_DEGREES + extraRotation);

    spinTimeoutRef.current = window.setTimeout(() => {
      setPickedMovie(movie);
      setIsSpinning(false);
      onSelectMovie(movie);
    }, SPIN_DURATION_MS);
  };

  return (
    <aside className="compass-pick-panel" aria-labelledby="compass-pick-heading">
      <div className="compass-stage" aria-hidden="true">
        <div className={`compass-ring${isSpinning ? " compass-ring-spinning" : ""}`}>
          <img
            className="compass-mark"
            src="/compass.svg"
            alt=""
            style={{ transform: `rotate(${compassRotation}deg)` }}
          />
          <span className="compass-scan-line" />
        </div>
      </div>

      <div className="compass-copy">
        <p className="panel-kicker">Tonight's compass</p>
        <h2 id="compass-pick-heading">Let the compass choose</h2>
        <p className="compass-status">{getCompassStatus(movies.length, isCatalogLoading, hasCatalogError)}</p>
      </div>

      <div className="compass-actions">
        <button type="button" className="compass-random-button" disabled={!canSpin} onClick={handleSpin}>
          {isSpinning ? "Spinning..." : "Spin for a pick"}
        </button>
      </div>

      <div className="compass-result" aria-live="polite">
        {isSpinning && <p className="compass-result-empty">Finding a pick...</p>}
        {!isSpinning && pickedMovie && (
          <button
            type="button"
            className={`compass-result-card${pickedMovie.movieId === selectedMovieId ? " compass-result-card-active" : ""}`}
            onClick={() => onSelectMovie(pickedMovie)}
            aria-label={`Open compass pick ${pickedMovie.title}`}
            aria-pressed={pickedMovie.movieId === selectedMovieId}
          >
            <PosterArtwork title={pickedMovie.title} posterUrl={pickedMovie.posterUrl} className="compass-result-poster" />
            <span>
              <span className="compass-result-label">Compass picked {pickedMovie.title}.</span>
              <span className="compass-result-meta">{formatMovieMeta(pickedMovie)}</span>
            </span>
          </button>
        )}
        {!isSpinning && !pickedMovie && (
          <p className="compass-result-empty">Load a feed, search, or recommendation list, then spin for a pick.</p>
        )}
      </div>
    </aside>
  );
}

function getCompassStatus(movieCount: number, isCatalogLoading: boolean, hasCatalogError: boolean): string {
  if (movieCount > 0) {
    return `${movieCount} current ${movieCount === 1 ? "title" : "titles"} ready for a random pick.`;
  }

  if (isCatalogLoading) {
    return "Loading movies from the active catalog views.";
  }

  if (hasCatalogError) {
    return "Connect the API or retry a feed to unlock random picks.";
  }

  return "No movies are loaded yet.";
}
