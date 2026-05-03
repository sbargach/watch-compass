import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import {
  discoverMovies,
  getApiBaseUrl,
  getGenres,
  getMovieDetails,
  getRecommendations,
  getSimilarMovies,
  getTrendingMovies,
  searchMovies
} from "./api/moviesApi";
import { MovieDetailsPanel } from "./components/MovieDetailsPanel";
import { MovieGrid } from "./components/MovieGrid";
import { Pagination } from "./components/Pagination";
import { RecommendationGrid } from "./components/RecommendationGrid";
import type {
  Mood,
  MovieCard,
  MovieDetails,
  Recommendation,
  RecommendationsRequest,
  SearchMoviesResponse
} from "./types/movies";

const RESULTS_PAGE_SIZE = 12;
const TRENDING_LIMIT = 12;
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

type TrendingState = {
  items: MovieCard[];
  isLoading: boolean;
  error: string | null;
};

type SearchState = {
  result: SearchMoviesResponse | null;
  isLoading: boolean;
  error: string | null;
};

type DetailsState = {
  details: MovieDetails | null;
  similarMovies: MovieCard[];
  isLoading: boolean;
  isSimilarLoading: boolean;
  error: string | null;
  similarError: string | null;
};

type GenresState = {
  items: string[];
  isLoading: boolean;
  error: string | null;
};

type RecommendationState = {
  items: Recommendation[];
  isLoading: boolean;
  error: string | null;
  hasRequested: boolean;
  appliedFilterKey: string | null;
};

type RecommendationFormState = {
  mood: Mood;
  timeBudgetMinutes: string;
  query: string;
  avoidGenres: string[];
};

type ReleaseYearFieldState = {
  releaseYear: number | null;
  validationMessage: string | null;
  statusLabel: string;
};

function createEmptyDetailsState(): DetailsState {
  return {
    details: null,
    similarMovies: [],
    isLoading: false,
    isSimilarLoading: false,
    error: null,
    similarError: null
  };
}

function createLoadingDetailsState(): DetailsState {
  return {
    details: null,
    similarMovies: [],
    isLoading: true,
    isSimilarLoading: true,
    error: null,
    similarError: null
  };
}

function createInitialRecommendationFormState(): RecommendationFormState {
  return {
    mood: "FeelGood",
    timeBudgetMinutes: "120",
    query: "",
    avoidGenres: []
  };
}

function createInitialRecommendationState(): RecommendationState {
  return {
    items: [],
    isLoading: false,
    error: null,
    hasRequested: false,
    appliedFilterKey: null
  };
}

function App() {
  const [queryInput, setQueryInput] = useState("");
  const [releaseYearInput, setReleaseYearInput] = useState("");
  const [activeQuery, setActiveQuery] = useState("");
  const [searchPage, setSearchPage] = useState(1);
  const [searchNonce, setSearchNonce] = useState(0);
  const [trendingState, setTrendingState] = useState<TrendingState>({
    items: [],
    isLoading: true,
    error: null
  });
  const [searchState, setSearchState] = useState<SearchState>({
    result: null,
    isLoading: false,
    error: null
  });
  const [discoverGenre, setDiscoverGenre] = useState<string | null>(null);
  const [discoverPage, setDiscoverPage] = useState(1);
  const [discoverState, setDiscoverState] = useState<SearchState>({
    result: null,
    isLoading: false,
    error: null
  });
  const [selectedMovie, setSelectedMovie] = useState<MovieCard | null>(null);
  const [detailsState, setDetailsState] = useState<DetailsState>(() => createEmptyDetailsState());
  const [genresState, setGenresState] = useState<GenresState>({
    items: [],
    isLoading: true,
    error: null
  });
  const [watchRegion, setWatchRegion] = useState(DEFAULT_WATCH_REGION);
  const [recommendationForm, setRecommendationForm] = useState<RecommendationFormState>(() =>
    createInitialRecommendationFormState()
  );
  const [recommendationState, setRecommendationState] = useState<RecommendationState>(() =>
    createInitialRecommendationState()
  );
  const releaseYearFieldState = getReleaseYearFieldState(releaseYearInput);
  const releaseYear = releaseYearFieldState.releaseYear;
  const releaseYearValidationMessage = releaseYearFieldState.validationMessage;
  const isReleaseYearValid = releaseYearValidationMessage === null;
  const recommendationFilterKey = isReleaseYearValid
    ? `valid:${releaseYear ?? "all"}`
    : `invalid:${releaseYearInput.trim()}`;

  useEffect(() => {
    let isActive = true;
    const loadTrending = async () => {
      try {
        const response = await getTrendingMovies(TRENDING_LIMIT);
        if (!isActive) {
          return;
        }

        setTrendingState({
          items: response.items,
          isLoading: false,
          error: null
        });
      } catch (error) {
        if (!isActive) {
          return;
        }

        setTrendingState({
          items: [],
          isLoading: false,
          error: toErrorMessage(error)
        });
      }
    };

    void loadTrending();

    return () => {
      isActive = false;
    };
  }, []);

  useEffect(() => {
    let isActive = true;

    void getGenres()
      .then((response) => {
        if (!isActive) {
          return;
        }

        setGenresState({
          items: response.items,
          isLoading: false,
          error: null
        });
      })
      .catch((error: unknown) => {
        if (!isActive) {
          return;
        }

        setGenresState({
          items: [],
          isLoading: false,
          error: toErrorMessage(error)
        });
      });

    return () => {
      isActive = false;
    };
  }, []);

  useEffect(() => {
    if (activeQuery.length === 0 || !isReleaseYearValid) {
      return;
    }

    let isActive = true;

    const loadSearchResults = async () => {
      setSearchState({
        result: null,
        isLoading: true,
        error: null
      });

      try {
        const response = await searchMovies(
          activeQuery,
          searchPage,
          RESULTS_PAGE_SIZE,
          releaseYear ?? undefined
        );
        if (!isActive) {
          return;
        }

        setSearchState({
          result: response,
          isLoading: false,
          error: null
        });
      } catch (error) {
        if (!isActive) {
          return;
        }

        setSearchState({
          result: null,
          isLoading: false,
          error: toErrorMessage(error)
        });
      }
    };

    void loadSearchResults();

    return () => {
      isActive = false;
    };
  }, [activeQuery, isReleaseYearValid, releaseYear, searchPage, searchNonce]);

  useEffect(() => {
    if (discoverGenre === null || !isReleaseYearValid) {
      return;
    }

    let isActive = true;

    const loadDiscoverResults = async () => {
      setDiscoverState({
        result: null,
        isLoading: true,
        error: null
      });

      try {
        const response = await discoverMovies(
          discoverGenre,
          discoverPage,
          RESULTS_PAGE_SIZE,
          releaseYear ?? undefined
        );
        if (!isActive) {
          return;
        }

        setDiscoverState({
          result: response,
          isLoading: false,
          error: null
        });
      } catch (error) {
        if (!isActive) {
          return;
        }

        setDiscoverState({
          result: null,
          isLoading: false,
          error: toErrorMessage(error)
        });
      }
    };

    void loadDiscoverResults();

    return () => {
      isActive = false;
    };
  }, [discoverGenre, discoverPage, isReleaseYearValid, releaseYear]);

  const selectedMovieId = selectedMovie?.movieId ?? null;
  const watchRegionLabel = getWatchRegionLabel(watchRegion);

  useEffect(() => {
    if (selectedMovieId === null) {
      return;
    }

    let isActive = true;

    setDetailsState((current) => ({
      ...current,
      details: current.details?.movieId === selectedMovieId ? current.details : null,
      isLoading: true,
      error: null
    }));

    void getMovieDetails(selectedMovieId, watchRegion)
      .then((details) => {
        if (!isActive) {
          return;
        }

        setDetailsState((current) => ({
          ...current,
          details,
          isLoading: false
        }));
      })
      .catch((error: unknown) => {
        if (!isActive) {
          return;
        }

        setDetailsState((current) => ({
          ...current,
          details: null,
          isLoading: false,
          error: toErrorMessage(error)
        }));
      });

    return () => {
      isActive = false;
    };
  }, [selectedMovieId, watchRegion]);

  useEffect(() => {
    if (selectedMovieId === null) {
      return;
    }

    let isActive = true;

    setDetailsState((current) => ({
      ...current,
      isSimilarLoading: true,
      similarError: null
    }));

    void getSimilarMovies(selectedMovieId)
      .then((response) => {
        if (!isActive) {
          return;
        }

        setDetailsState((current) => ({
          ...current,
          similarMovies: response.items,
          isSimilarLoading: false
        }));
      })
      .catch((error: unknown) => {
        if (!isActive) {
          return;
        }

        setDetailsState((current) => ({
          ...current,
          similarMovies: [],
          isSimilarLoading: false,
          similarError: toErrorMessage(error)
        }));
      });

    return () => {
      isActive = false;
    };
  }, [selectedMovieId]);

  const clearSelectedMovie = () => {
    setSelectedMovie(null);
    setDetailsState(createEmptyDetailsState());
  };

  const clearDiscoverSelection = () => {
    setDiscoverGenre(null);
    setDiscoverPage(1);
    setDiscoverState({
      result: null,
      isLoading: false,
      error: null
    });
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
    setDetailsState(createLoadingDetailsState());
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
      setSearchState({
        result: null,
        isLoading: false,
        error: null
      });
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

  const handleRecommendationSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const requestFilterKey = recommendationFilterKey;

    const parsedTimeBudget = Number.parseInt(recommendationForm.timeBudgetMinutes, 10);
    if (Number.isNaN(parsedTimeBudget) || parsedTimeBudget < 1 || parsedTimeBudget > 600) {
      setRecommendationState({
        items: [],
        isLoading: false,
        error: "Time budget must be between 1 and 600 minutes.",
        hasRequested: true,
        appliedFilterKey: requestFilterKey
      });
      return;
    }

    if (releaseYearValidationMessage !== null) {
      setRecommendationState({
        items: [],
        isLoading: false,
        error: releaseYearValidationMessage,
        hasRequested: true,
        appliedFilterKey: requestFilterKey
      });
      return;
    }

    const request: RecommendationsRequest = {
      mood: recommendationForm.mood,
      timeBudgetMinutes: parsedTimeBudget,
      query: recommendationForm.query.trim() || undefined,
      avoidGenres: recommendationForm.avoidGenres,
      releaseYear: releaseYear ?? undefined,
      countryCode: watchRegion
    };

    clearSelectedMovie();
    setRecommendationState({
      items: [],
      isLoading: true,
      error: null,
      hasRequested: true,
      appliedFilterKey: requestFilterKey
    });

    try {
      const response = await getRecommendations(request);
      setRecommendationState({
        items: response.items,
        isLoading: false,
        error: null,
        hasRequested: true,
        appliedFilterKey: requestFilterKey
      });
    } catch (error) {
      setRecommendationState({
        items: [],
        isLoading: false,
        error: toErrorMessage(error),
        hasRequested: true,
        appliedFilterKey: requestFilterKey
      });
    }
  };

  const hasSearch = activeQuery.length > 0;
  const hasSearchResults = (searchState.result?.items.length ?? 0) > 0;
  const hasDiscoverResults = (discoverState.result?.items.length ?? 0) > 0;
  const recommendationStateMatchesFilter = recommendationState.appliedFilterKey === recommendationFilterKey;
  const hasRecommendations = recommendationStateMatchesFilter && recommendationState.items.length > 0;

  return (
    <div className="app-shell">
      <div className="app-aurora app-aurora-left" />
      <div className="app-aurora app-aurora-right" />

      <main className="app-content">
        <header className="hero">
          <p className="hero-kicker">Watch Compass</p>
          <h1>Movie Discovery Desk</h1>
          <p className="hero-description">
            Frontend integration for trending titles, search, and recommendation workflows on top of your API.
          </p>
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
              <select value={watchRegion} onChange={(event) => setWatchRegion(event.target.value)}>
                {WATCH_REGIONS.map((region) => (
                  <option key={region.code} value={region.code}>
                    {region.label} ({region.code})
                  </option>
                ))}
              </select>
            </label>
          </div>

          <p className="api-target">
            API: {getApiBaseUrl()} {" | "} Provider availability uses {watchRegionLabel}. {" | "} Release year:{" "}
            {releaseYearFieldState.statusLabel}.
          </p>
          {releaseYearValidationMessage && <p className="status-text status-error">{releaseYearValidationMessage}</p>}
        </section>

        <section className="recommendation-panel">
          <div className="recommendation-panel-header">
            <div>
              <p className="panel-kicker">Recommendation Studio</p>
              <h2>Recommendation Builder</h2>
            </div>
            <p className="recommendation-panel-copy">
              Shape a mood-based recommendation run with time budget, country context, and genre exclusions.
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
                placeholder="Actor, franchise, vibe..."
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
              {genresState.error && <p className="status-text status-error">{genresState.error}</p>}
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
                {recommendationState.isLoading ? "Curating..." : "Get recommendations"}
              </button>
            </div>
          </form>

          {recommendationStateMatchesFilter && recommendationState.error && (
            <p className="status-text status-error">{recommendationState.error}</p>
          )}
          {recommendationStateMatchesFilter && recommendationState.isLoading && (
            <p className="status-text">Loading recommendations...</p>
          )}
          {recommendationStateMatchesFilter &&
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
                  {recommendationState.items.length} picks for <strong>{formatMoodLabel(recommendationForm.mood)}</strong>.
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
                Pick a genre to browse a popularity-sorted catalog slice from the backend. The toolbar release-year
                filter also narrows this view. Results reuse the shared details panel and pagination flow.
              </p>
            </div>

            {genresState.isLoading && <p className="status-text">Loading genres...</p>}
            {genresState.error && <p className="status-text status-error">{genresState.error}</p>}
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
            {isReleaseYearValid && discoverState.error && <p className="status-text status-error">{discoverState.error}</p>}
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
                    {discoverState.result?.totalResults ?? 0} results
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
          <section className="content-section">
            <div className="section-heading">
              <h2>Trending Today</h2>
              <p>Fresh picks from the TMDB-backed API. Select a card to open details and similar titles.</p>
            </div>

            {trendingState.isLoading && <p className="status-text">Loading trending movies...</p>}
            {trendingState.error && <p className="status-text status-error">{trendingState.error}</p>}
            {!trendingState.isLoading && !trendingState.error && trendingState.items.length === 0 && (
              <p className="status-text">No trending movies were returned.</p>
            )}
            {trendingState.items.length > 0 && (
              <MovieGrid
                movies={trendingState.items}
                onSelectMovie={handleSelectMovie}
                selectedMovieId={selectedMovie?.movieId}
              />
            )}
          </section>
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
                  ` (${searchState.result.totalResults} results${releaseYear !== null ? ` in ${releaseYear}` : ""}). Select a card for deeper context.`}
              </p>
            </div>

            {isReleaseYearValid && searchState.error && <p className="status-text status-error">{searchState.error}</p>}
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

function toErrorMessage(error: unknown): string {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return "Unexpected error while calling the API.";
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
