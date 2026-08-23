import { MovieGrid } from "../../components/MovieGrid";
import type { MovieCard } from "../../types/movies";

type Props = {
  title: string;
  description: string;
  loadingLabel: string;
  emptyLabel: string;
  retryLabel: string;
  state: { items: MovieCard[]; isLoading: boolean; error: string | null };
  selectedMovieId?: number | null;
  onSelectMovie: (movie: MovieCard) => void;
  onRetry: () => void;
};

export function CatalogFeedSection({
  title,
  description,
  loadingLabel,
  emptyLabel,
  retryLabel,
  state,
  selectedMovieId,
  onSelectMovie,
  onRetry
}: Props) {
  return (
    <section className="content-section">
      <div className="section-heading">
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      {state.isLoading && <p className="status-text">{loadingLabel}</p>}
      {state.error && (
        <div className="recovery-action" role="alert">
          <p className="status-text status-error">{state.error}</p>
          <button type="button" className="secondary-button" onClick={onRetry}>{retryLabel}</button>
        </div>
      )}
      {!state.isLoading && !state.error && state.items.length === 0 && (
        <p className="status-text">{emptyLabel}</p>
      )}
      {state.items.length > 0 && (
        <MovieGrid movies={state.items} onSelectMovie={onSelectMovie} selectedMovieId={selectedMovieId} />
      )}
    </section>
  );
}
