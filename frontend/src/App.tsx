import { useEffect, useState, type FormEvent } from "react";
import { getApiBaseUrl, getTrendingMovies, searchMovies } from "./api/moviesApi";
import { MovieGrid } from "./components/MovieGrid";
import { Pagination } from "./components/Pagination";
import type { MovieCard, SearchMoviesResponse } from "./types/movies";

const SEARCH_PAGE_SIZE = 12;
const TRENDING_LIMIT = 12;

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

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmedQuery = queryInput.trim();

    if (trimmedQuery.length === 0) {
      setActiveQuery("");
      setSearchPage(1);
      setSearchState({
        result: null,
        isLoading: false,
        error: null
      });
      return;
    }

    setSearchPage(1);
    setActiveQuery(trimmedQuery);
    setSearchNonce((current) => current + 1);
  };

  const hasSearch = activeQuery.length > 0;
  const hasSearchResults = (searchState.result?.items.length ?? 0) > 0;

  return (
    <div className="app-shell">
      <div className="app-aurora app-aurora-left" />
      <div className="app-aurora app-aurora-right" />

      <main className="app-content">
        <header className="hero">
          <p className="hero-kicker">Watch Compass</p>
          <h1>Movie Discovery Desk</h1>
          <p className="hero-description">
            Frontend integration for trending titles and paginated search on top of your API.
          </p>
        </header>

        <section className="search-panel">
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
          <p className="api-target">API: {getApiBaseUrl()}</p>
        </section>

        {!hasSearch && (
          <section className="content-section">
            <div className="section-heading">
              <h2>Trending Today</h2>
              <p>Fresh picks from the TMDB-backed API.</p>
            </div>

            {trendingState.isLoading && <p className="status-text">Loading trending movies...</p>}
            {trendingState.error && <p className="status-text status-error">{trendingState.error}</p>}
            {!trendingState.isLoading && !trendingState.error && trendingState.items.length === 0 && (
              <p className="status-text">No trending movies were returned.</p>
            )}
            {trendingState.items.length > 0 && <MovieGrid movies={trendingState.items} />}
          </section>
        )}

        {hasSearch && (
          <section className="content-section">
            <div className="section-heading">
              <h2>Search Results</h2>
              <p>
                Query: <strong>{activeQuery}</strong>
                {searchState.result && ` (${searchState.result.totalResults} results)`}
              </p>
            </div>

            {searchState.error && <p className="status-text status-error">{searchState.error}</p>}
            {searchState.isLoading && <p className="status-text">Loading page {searchPage}...</p>}
            {!searchState.isLoading && !searchState.error && !hasSearchResults && (
              <p className="status-text">No movies matched your query.</p>
            )}
            {hasSearchResults && (
              <>
                <MovieGrid movies={searchState.result?.items ?? []} />
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

function toErrorMessage(error: unknown): string {
  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return "Unexpected error while calling the API.";
}

export default App;
