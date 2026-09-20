type Props = {
  mode: "demo" | "tmdb" | undefined;
  error: string | null;
  isLoading: boolean;
  onRetry: () => void;
};

export function CatalogModeNotice({ mode, error, isLoading, onRetry }: Props) {
  if (isLoading) return <p className="catalog-notice" role="status">Checking catalog mode...</p>;
  if (error) {
    return (
      <div className="catalog-notice" role="alert">
        <p>{error}</p>
        <button type="button" className="secondary-button" onClick={onRetry}>Retry catalog status</button>
      </div>
    );
  }
  if (mode !== "demo") return null;
  return (
    <aside className="catalog-notice" aria-label="Demo catalog">
      <strong>Demo mode — fictional films, real app.</strong>
      <p>Explore 24 sample films without an API key. Feeds are fixed examples; streaming availability is simulated.</p>
    </aside>
  );
}
