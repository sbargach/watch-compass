import { useEffect, useState, type FormEvent } from "react";
import {
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

const SEARCH_PAGE_SIZE = 12;
const TRENDING_LIMIT = 12;
const MOODS: readonly Mood[] = ["FeelGood", "Chill", "Intense", "Scary"];
const DEFAULT_WATCH_REGION = "NL";
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
};

type RecommendationFormState = {
  mood: Mood;
  timeBudgetMinutes: string;
  query: string;
  avoidGenres: string[];
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

function App() {
  const [queryInput, setQueryInput] = useState("");
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
  const [recommendationState, setRecommendationState] = useState<RecommendationState>({
    items: [],
    isLoading: false,
    error: null,
    hasRequested: false
  });

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
    if (activeQuery.length === 0) {
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
        const response = await searchMovies(activeQuery, searchPage, SEARCH_PAGE_SIZE);
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
  }, [activeQuery, searchPage, searchNonce]);

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

  const handleSelectMovie = (movie: MovieCard) => {
    if (selectedMovie?.movieId === movie.movieId) {
      return;
    }

    setSelectedMovie(movie);
    setDetailsState(createLoadingDetailsState());
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

    clearSelectedMovie();
    setSearchPage(1);
    setActiveQuery(trimmedQuery);
    setSearchNonce((current) => current + 1);
  };

  const handleRecommendationSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const parsedTimeBudget = Number.parseInt(recommendationForm.timeBudgetMinutes, 10);
    if (Number.isNaN(parsedTimeBudget) || parsedTimeBudget < 1 || parsedTimeBudget > 600) {
      setRecommendationState({
        items: [],
        isLoading: false,
        error: "Time budget must be between 1 and 600 minutes.",
        hasRequested: true
      });
      return;
    }

    const request: RecommendationsRequest = {
      mood: recommendationForm.mood,
      timeBudgetMinutes: parsedTimeBudget,
      query: recommendationForm.query.trim() || undefined,
      avoidGenres: recommendationForm.avoidGenres,
      countryCode: watchRegion
    };

    clearSelectedMovie();
    setRecommendationState({
      items: [],
      isLoading: true,
      error: null,
      hasRequested: true
    });

    try {
      const response = await getRecommendations(request);
      setRecommendationState({
        items: response.items,
        isLoading: false,
        error: null,
        hasRequested: true
      });
    } catch (error) {
      setRecommendationState({
        items: [],
        isLoading: false,
        error: toErrorMessage(error),
        hasRequested: true
      });
    }
  };

  const hasSearch = activeQuery.length > 0;
  const hasSearchResults = (searchState.result?.items.length ?? 0) > 0;
  const hasRecommendations = recommendationState.items.length > 0;

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
                type="search"
                placeholder="Search movies by title..."
                value={queryInput}
                onChange={(event) => setQueryInput(event.target.value)}
                aria-label="Search movies"
              />
              <button type="submit" disabled={searchState.isLoading}>
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
            API: {getApiBaseUrl()} {" | "} Provider availability uses {watchRegionLabel}.
          </p>
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
              <button type="submit" disabled={recommendationState.isLoading}>
                {recommendationState.isLoading ? "Curating..." : "Get recommendations"}
              </button>
            </div>
          </form>

          {recommendationState.error && <p className="status-text status-error">{recommendationState.error}</p>}
          {recommendationState.isLoading && <p className="status-text">Loading recommendations...</p>}
          {recommendationState.hasRequested && !recommendationState.isLoading && !recommendationState.error && !hasRecommendations && (
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
                {searchState.result && ` (${searchState.result.totalResults} results). Select a card for deeper context.`}
              </p>
            </div>

            {searchState.error && <p className="status-text status-error">{searchState.error}</p>}
            {searchState.isLoading && <p className="status-text">Loading page {searchPage}...</p>}
            {!searchState.isLoading && !searchState.error && !hasSearchResults && (
              <p className="status-text">No movies matched your query.</p>
            )}
            {hasSearchResults && (
              <>
                <MovieGrid
                  movies={searchState.result?.items ?? []}
                  onSelectMovie={handleSelectMovie}
                  selectedMovieId={selectedMovie?.movieId}
                />
                <Pagination
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

export default App;
