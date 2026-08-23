import { useState, type ChangeEvent, type FormEvent } from "react";
import { MovieDetailsPanel } from "./components/MovieDetailsPanel";
import { MovieGrid } from "./components/MovieGrid";
import { Pagination } from "./components/Pagination";
import { RecommendationGrid } from "./components/RecommendationGrid";
import { CreditsFooter } from "./components/CreditsFooter";
import { useCatalogOverview } from "./features/catalog/useCatalogOverview";
import { CatalogFeedSection } from "./features/catalog/CatalogFeedSection";
import { useMovieDetails } from "./features/details/useMovieDetails";
import { usePagedMovies } from "./features/browse/usePagedMovies";
import { useRecommendations } from "./features/recommendations/useRecommendations";
import type {
  Mood,
  MovieCard,
  Recommendation
} from "./types/movies";

const RESULTS_PAGE_SIZE = 12;
const TRENDING_LIMIT = 12;
const NOW_PLAYING_LIMIT = 12;
const MOODS: readonly Mood[] = ["FeelGood", "Chill", "Intense", "Scary"];
const DEFAULT_WATCH_REGION = "NL";
const MIN_RELEASE_YEAR = 1888;
const MAX_RELEASE_YEAR = new Date().getUTCFullYear() + 1;
const WATCH_REGIONS = [
  { code: "NL", label: "Netherlands" },
  { code: "US", label: "United States" },
  { code: "GB", label: "United Kingdom" },
  { code: "DE", label: "Germany" },
  { code: "FR", label: "France" },
  { code: "BE", label: "Belgium" },
  { code: "CA", label: "Canada" }
] as const;

type ReleaseYearFieldState = {
  releaseYear: number | null;
  validationMessage: string | null;
  statusLabel: string;
};

function App() {
  const [queryInput, setQueryInput] = useState("");
  const [releaseYearInput, setReleaseYearInput] = useState("");
  const [activeQuery, setActiveQuery] = useState("");
  const [searchPage, setSearchPage] = useState(1);
  const [searchNonce, setSearchNonce] = useState(0);
  const {
    trendingState,
    nowPlayingState,
    genresState,
    retryTrending,
    retryNowPlaying,
    retryGenres
  } = useCatalogOverview(
    TRENDING_LIMIT,
    NOW_PLAYING_LIMIT
  );
  const [discoverGenre, setDiscoverGenre] = useState<string | null>(null);
  const [discoverPage, setDiscoverPage] = useState(1);
  const [selectedMovie, setSelectedMovie] = useState<MovieCard | null>(null);
  const [watchRegion, setWatchRegion] = useState(DEFAULT_WATCH_REGION);
  const releaseYearFieldState = getReleaseYearFieldState(releaseYearInput);
  const releaseYear = releaseYearFieldState.releaseYear;
  const releaseYearValidationMessage = releaseYearFieldState.validationMessage;
  const isReleaseYearValid = releaseYearValidationMessage === null;
  const {
    recommendationForm,
    setRecommendationForm,
    recommendationState,
    recommendationStateMatchesRequest,
    handleRecommendationSubmit
  } = useRecommendations({
    watchRegion,
    releaseYearInput,
    releaseYear,
    releaseYearValidationMessage,
    onBeforeRequest: () => setSelectedMovie(null)
  });
  const searchState = usePagedMovies({
    mode: "search",
    value: activeQuery || null,
    page: searchPage,
    pageSize: RESULTS_PAGE_SIZE,
    releaseYear: releaseYear ?? undefined,
    isValid: isReleaseYearValid,
    reloadKey: searchNonce
  });
  const discoverState = usePagedMovies({
    mode: "discover",
    value: discoverGenre,
    page: discoverPage,
    pageSize: RESULTS_PAGE_SIZE,
    releaseYear: releaseYear ?? undefined,
    isValid: isReleaseYearValid
  });

  const selectedMovieId = selectedMovie?.movieId ?? null;
  const detailsState = useMovieDetails(selectedMovieId, watchRegion);
  const watchRegionLabel = getWatchRegionLabel(watchRegion);

  const handleWatchRegionChange = (event: ChangeEvent<HTMLSelectElement>) => {
    setWatchRegion(event.target.value);

  };

  const clearSelectedMovie = () => {
    setSelectedMovie(null);
  };

  const clearDiscoverSelection = () => {
    setDiscoverGenre(null);
    setDiscoverPage(1);
  };

  const handleSelectDiscoverGenre = (genre: string) => {
    clearSelectedMovie();

    if (discoverGenre === genre) {
      clearDiscoverSelection();
      return;
    }

    setDiscoverGenre(genre);
    setDiscoverPage(1);
  };

  const handleSelectMovie = (movie: MovieCard) => {
    if (selectedMovie?.movieId === movie.movieId) {
      return;
    }

    setSelectedMovie(movie);
  };

  const handleReleaseYearChange = (event: ChangeEvent<HTMLInputElement>) => {
    const nextInput = event.target.value;
    const currentFieldState = getReleaseYearFieldState(releaseYearInput);
    const nextFieldState = getReleaseYearFieldState(nextInput);

    setReleaseYearInput(nextInput);

    if (
      nextFieldState.validationMessage !== null ||
      currentFieldState.releaseYear === nextFieldState.releaseYear
    ) {
      return;
    }

    if (activeQuery.length > 0) {
      setSearchPage(1);
    }

    if (discoverGenre !== null) {
      setDiscoverPage(1);
    }
  };

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmedQuery = queryInput.trim();

    if (trimmedQuery.length === 0) {
      clearSelectedMovie();
      setActiveQuery("");
      setSearchPage(1);
      return;
    }

    if (releaseYearValidationMessage !== null) {
      return;
    }

    clearSelectedMovie();
    setSearchPage(1);
    setActiveQuery(trimmedQuery);
    setSearchNonce((current) => current + 1);
  };

  const hasSearch = activeQuery.length > 0;
  const hasSearchResults = (searchState.result?.items.length ?? 0) > 0;
  const hasDiscoverResults = (discoverState.result?.items.length ?? 0) > 0;
  const hasRecommendations = recommendationStateMatchesRequest && recommendationState.items.length > 0;

  return (
    <div className="app-shell">
      <main className="app-content">
        <header className="hero">
          <div className="hero-copy">
            <p className="hero-kicker">Watch Compass</p>
            <h1>Find something worth watching.</h1>
            <p className="hero-description">
              Search movies, browse current releases, and build a shortlist around your mood and available time.
            </p>
          </div>
        </header>

        <section className="search-panel">
          <div className="search-toolbar">
            <form className="search-form" onSubmit={handleSubmit}>
              <input
                className="search-query-input"
                type="search"
                placeholder="Search movies by title..."
                value={queryInput}
                onChange={(event) => setQueryInput(event.target.value)}
                aria-label="Search movies"
              />
              <label className="field search-year-field">
                <span>Release year</span>
                <input
                  className="search-year-input"
                  type="number"
                  min={MIN_RELEASE_YEAR}
                  max={MAX_RELEASE_YEAR}
                  placeholder="Any year"
                  value={releaseYearInput}
                  onChange={handleReleaseYearChange}
                  aria-label="Release year"
                  aria-invalid={releaseYearValidationMessage !== null}
                />
              </label>
              <button type="submit" disabled={searchState.isLoading || !isReleaseYearValid}>
                {searchState.isLoading ? "Searching..." : "Search"}
              </button>
            </form>

            <label className="field search-region-field">
              <span>Watch region</span>
              <select value={watchRegion} onChange={handleWatchRegionChange}>
                {WATCH_REGIONS.map((region) => (
                  <option key={region.code} value={region.code}>
                    {region.label} ({region.code})
                  </option>
                ))}
              </select>
            </label>
          </div>

          {releaseYearValidationMessage && <p className="status-text status-error" role="alert">{releaseYearValidationMessage}</p>}
        </section>

        <section className="recommendation-panel">
          <div className="recommendation-panel-header">
            <div>
              <p className="panel-kicker">Recommendations</p>
              <h2>Build a shortlist</h2>
            </div>
            <p className="recommendation-panel-copy">
              Choose a mood, set your available time, and exclude anything you do not want to watch.
            </p>
          </div>

          <form className="recommendation-form" onSubmit={handleRecommendationSubmit}>
            <label className="field">
              <span>Mood</span>
              <select
                value={recommendationForm.mood}
                onChange={(event) =>
                  setRecommendationForm((current) => ({
                    ...current,
                    mood: event.target.value as Mood
                  }))
                }
              >
                {MOODS.map((mood) => (
                  <option key={mood} value={mood}>
                    {formatMoodLabel(mood)}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <span>Time budget (minutes)</span>
              <input
                type="number"
                min={1}
                max={600}
                value={recommendationForm.timeBudgetMinutes}
                onChange={(event) =>
                  setRecommendationForm((current) => ({
                    ...current,
                    timeBudgetMinutes: event.target.value
                  }))
                }
              />
            </label>

            <div className="field">
              <span>Watch region</span>
              <p className="field-note">{watchRegionLabel} from the main toolbar.</p>
            </div>

            <div className="field">
              <span>Release year</span>
              <p className="field-note">{releaseYearFieldState.statusLabel} from the main toolbar.</p>
            </div>

            <label className="field field-wide">
              <span>Optional hint</span>
              <input
                type="text"
                placeholder="Actor, franchise, or title..."
                value={recommendationForm.query}
                onChange={(event) =>
                  setRecommendationForm((current) => ({
                    ...current,
                    query: event.target.value
                  }))
                }
              />
            </label>

            <div className="field field-full">
              <span>Avoid genres</span>
              {genresState.isLoading && <p className="status-text">Loading genres...</p>}
              {genresState.error && (
                <div className="recovery-action" role="alert">
                  <p className="status-text status-error">{genresState.error}</p>
                  <button type="button" className="secondary-button" onClick={retryGenres}>Retry genres</button>
                </div>
              )}
              {!genresState.isLoading && !genresState.error && genresState.items.length === 0 && (
                <p className="status-text">No genres were returned.</p>
              )}
              {genresState.items.length > 0 && (
                <div className="genre-chip-list">
                  {genresState.items.map((genre) => {
                    const isSelected = recommendationForm.avoidGenres.includes(genre);

                    return (
                      <button
                        key={genre}
                        type="button"
                        className={`genre-chip${isSelected ? " genre-chip-selected" : ""}`}
                        aria-label={`Avoid genre ${genre}`}
                        onClick={() =>
                          setRecommendationForm((current) => ({
                            ...current,
                            avoidGenres: current.avoidGenres.includes(genre)
                              ? current.avoidGenres.filter((item) => item !== genre)
                              : [...current.avoidGenres, genre]
                          }))
                        }
                        aria-pressed={isSelected}
                      >
                        {genre}
                      </button>
                    );
                  })}
                </div>
              )}
            </div>

            <div className="recommendation-actions">
              <button type="submit" disabled={recommendationState.isLoading || !isReleaseYearValid}>
                {recommendationState.isLoading ? "Matching..." : "Get recommendations"}
              </button>
            </div>
          </form>

          {recommendationStateMatchesRequest && recommendationState.error && (
            <p className="status-text status-error" role="alert">{recommendationState.error}</p>
          )}
          {recommendationStateMatchesRequest && recommendationState.isLoading && (
            <p className="status-text">Loading recommendations...</p>
          )}
          {recommendationStateMatchesRequest &&
            recommendationState.hasRequested &&
            !recommendationState.isLoading &&
            !recommendationState.error &&
            !hasRecommendations && (
            <p className="status-text">No recommendations matched the current filters.</p>
          )}

          {hasRecommendations && (
            <div className="recommendation-results">
              <div className="section-heading recommendation-results-heading">
                <h2>Recommendation Results</h2>
                <p>
                  {formatCount(recommendationState.items.length, "pick")} for{" "}
                  <strong>{formatMoodLabel(recommendationForm.mood)}</strong>.
                </p>
              </div>

              <RecommendationGrid
                recommendations={recommendationState.items}
                selectedMovieId={selectedMovie?.movieId}
                onSelectRecommendation={(recommendation) => handleSelectMovie(toMovieCard(recommendation))}
              />
            </div>
          )}
        </section>

        {selectedMovie && (
          <MovieDetailsPanel
            selectedMovie={selectedMovie}
            details={detailsState.details}
            similarMovies={detailsState.similarMovies}
            isLoading={detailsState.isLoading}
            isSimilarLoading={detailsState.isSimilarLoading}
            error={detailsState.error}
            similarError={detailsState.similarError}
            watchRegionLabel={watchRegionLabel}
            onClose={clearSelectedMovie}
            onRetry={detailsState.retry}
            onSelectMovie={handleSelectMovie}
          />
        )}

        {!hasSearch && (
          <section className="genre-explorer">
            <div className="genre-explorer-header">
              <div>
                <p className="panel-kicker">Browse Mode</p>
                <h2>Genre Explorer</h2>
              </div>
              <p className="genre-explorer-copy">
                Pick a genre to browse popular movies. The release-year filter applies here too.
              </p>
            </div>

            {genresState.isLoading && <p className="status-text">Loading genres...</p>}
            {genresState.error && (
              <div className="recovery-action" role="alert">
                <p className="status-text status-error">{genresState.error}</p>
                <button type="button" className="secondary-button" onClick={retryGenres}>Retry genres</button>
              </div>
            )}
            {!genresState.isLoading && !genresState.error && genresState.items.length === 0 && (
              <p className="status-text">No genres were returned.</p>
            )}

            {genresState.items.length > 0 && (
              <div className="genre-explorer-actions">
                <div className="genre-chip-list">
                  {genresState.items.map((genre) => {
                    const isSelected = discoverGenre === genre;

                    return (
                      <button
                        key={genre}
                        type="button"
                        className={`genre-chip${isSelected ? " genre-chip-selected" : ""}`}
                        aria-label={`Browse genre ${genre}`}
                        onClick={() => handleSelectDiscoverGenre(genre)}
                        aria-pressed={isSelected}
                      >
                        {genre}
                      </button>
                    );
                  })}
                </div>

                {discoverGenre && (
                  <button type="button" className="secondary-button" onClick={clearDiscoverSelection}>
                    Clear genre
                  </button>
                )}
              </div>
            )}

            {!discoverGenre && !genresState.isLoading && !genresState.error && genresState.items.length > 0 && (
              <p className="status-text">Select a genre to load a dedicated discovery page.</p>
            )}
            {discoverGenre && !isReleaseYearValid && (
              <p className="status-text">Fix the release year to refresh this genre view.</p>
            )}
            {isReleaseYearValid && discoverState.error && <p className="status-text status-error" role="alert">{discoverState.error}</p>}
            {isReleaseYearValid && discoverState.isLoading && discoverGenre && (
              <p className="status-text">Loading {discoverGenre} picks...</p>
            )}
            {discoverGenre && isReleaseYearValid && !discoverState.isLoading && !discoverState.error && !hasDiscoverResults && (
              <p className="status-text">No movies were returned for this genre.</p>
            )}

            {hasDiscoverResults && isReleaseYearValid && (
              <div className="genre-results">
                <div className="section-heading genre-results-heading">
                  <h2>{discoverGenre} Picks</h2>
                  <p>
                    {formatCount(discoverState.result?.totalResults ?? 0, "result")}
                    {releaseYear !== null ? ` from ${releaseYear}` : ""}. Select a card for details and
                    similar titles.
                  </p>
                </div>

                <MovieGrid
                  movies={discoverState.result?.items ?? []}
                  onSelectMovie={handleSelectMovie}
                  selectedMovieId={selectedMovie?.movieId}
                />
                <Pagination
                  label="Genre pagination"
                  page={discoverState.result?.page ?? 1}
                  totalPages={discoverState.result?.totalPages ?? 1}
                  totalResults={discoverState.result?.totalResults ?? 0}
                  hasNextPage={discoverState.result?.hasNextPage ?? false}
                  disabled={discoverState.isLoading}
                  onPrevious={() => setDiscoverPage((current) => Math.max(1, current - 1))}
                  onNext={() => setDiscoverPage((current) => current + 1)}
                />
              </div>
            )}
          </section>
        )}

        {!hasSearch && (
          <CatalogFeedSection
            title="Now Playing"
            description="Movies currently playing in theaters."
            loadingLabel="Loading now playing movies..."
            emptyLabel="No now playing movies were returned."
            retryLabel="Retry now playing"
            state={nowPlayingState}
            selectedMovieId={selectedMovie?.movieId}
            onSelectMovie={handleSelectMovie}
            onRetry={retryNowPlaying}
          />
        )}

        {!hasSearch && (
          <CatalogFeedSection
            title="Trending Today"
            description="Movies people are watching today."
            loadingLabel="Loading trending movies..."
            emptyLabel="No trending movies were returned."
            retryLabel="Retry trending"
            state={trendingState}
            selectedMovieId={selectedMovie?.movieId}
            onSelectMovie={handleSelectMovie}
            onRetry={retryTrending}
          />
        )}

        {hasSearch && (
          <section className="content-section">
            <div className="section-heading">
              <h2>Search Results</h2>
              <p>
                Query: <strong>{activeQuery}</strong>
                {!isReleaseYearValid && " Fix the release year to refresh results."}
                {isReleaseYearValid &&
                  searchState.result &&
                  ` (${formatCount(searchState.result.totalResults, "result")}${releaseYear !== null ? ` in ${releaseYear}` : ""}). Select a card for deeper context.`}
              </p>
            </div>

            {isReleaseYearValid && searchState.error && <p className="status-text status-error" role="alert">{searchState.error}</p>}
            {isReleaseYearValid && searchState.isLoading && <p className="status-text">Loading page {searchPage}...</p>}
            {isReleaseYearValid && !searchState.isLoading && !searchState.error && !hasSearchResults && (
              <p className="status-text">No movies matched your query.</p>
            )}
            {hasSearchResults && isReleaseYearValid && (
              <>
                <MovieGrid
                  movies={searchState.result?.items ?? []}
                  onSelectMovie={handleSelectMovie}
                  selectedMovieId={selectedMovie?.movieId}
                />
                <Pagination
                  label="Search pagination"
                  page={searchState.result?.page ?? 1}
                  totalPages={searchState.result?.totalPages ?? 1}
                  totalResults={searchState.result?.totalResults ?? 0}
                  hasNextPage={searchState.result?.hasNextPage ?? false}
                  disabled={searchState.isLoading}
                  onPrevious={() => setSearchPage((current) => Math.max(1, current - 1))}
                  onNext={() => setSearchPage((current) => current + 1)}
                />
              </>
            )}
          </section>
        )}
        <CreditsFooter />
      </main>
    </div>
  );
}

function formatMoodLabel(mood: Mood): string {
  switch (mood) {
    case "FeelGood":
      return "Feel Good";
    default:
      return mood;
  }
}

function formatCount(count: number, singularNoun: string): string {
  return `${count} ${count === 1 ? singularNoun : `${singularNoun}s`}`;
}

function getWatchRegionLabel(countryCode: string): string {
  const region = WATCH_REGIONS.find((item) => item.code === countryCode);
  return region ? `${region.label} (${region.code})` : countryCode;
}

function toMovieCard(recommendation: Recommendation): MovieCard {
  return {
    movieId: recommendation.movieId,
    title: recommendation.title,
    runtimeMinutes: recommendation.runtimeMinutes,
    genres: recommendation.genres,
    posterUrl: recommendation.posterUrl,
    backdropUrl: recommendation.backdropUrl,
    releaseYear: recommendation.releaseYear,
    overview: recommendation.overview
  };
}

function getReleaseYearFieldState(input: string): ReleaseYearFieldState {
  const trimmedInput = input.trim();
  if (trimmedInput.length === 0) {
    return {
      releaseYear: null,
      validationMessage: null,
      statusLabel: "Any year"
    };
  }

  if (!/^\d{4}$/.test(trimmedInput)) {
    return {
      releaseYear: null,
      validationMessage: "Use a four-digit release year.",
      statusLabel: "Fix input"
    };
  }

  const releaseYear = Number.parseInt(trimmedInput, 10);
  if (releaseYear < MIN_RELEASE_YEAR || releaseYear > MAX_RELEASE_YEAR) {
    return {
      releaseYear: null,
      validationMessage: `Release year must be between ${MIN_RELEASE_YEAR} and ${MAX_RELEASE_YEAR}.`,
      statusLabel: "Fix input"
    };
  }

  return {
    releaseYear,
    validationMessage: null,
    statusLabel: String(releaseYear)
  };
}

export default App;
