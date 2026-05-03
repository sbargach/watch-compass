import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import App from "./App";

const trendingMovie = {
  movieId: 1,
  title: "Arrival",
  runtimeMinutes: 116,
  genres: ["Science Fiction", "Drama"],
  posterUrl: "https://image.tmdb.org/t/p/w500/arrival.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/arrival-backdrop.jpg",
  releaseYear: 2016,
  overview: "A linguist works to understand a new visitor."
};

const detailsMovieNl = {
  ...trendingMovie,
  providers: ["Videoland", "Apple TV"]
};

const detailsMovieUs = {
  ...trendingMovie,
  providers: ["Netflix", "Apple TV"]
};

const recommendedMovie = {
  movieId: 2,
  title: "Palm Springs",
  runtimeMinutes: 90,
  genres: ["Comedy", "Romance"],
  posterUrl: "https://image.tmdb.org/t/p/w500/palm-springs.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/palm-springs-backdrop.jpg",
  releaseYear: 2020,
  overview: "Two wedding guests get stuck in a time loop.",
  reasons: ["Matches the feel-good mood.", "Fits a short evening watch."],
  providers: ["Prime Video"]
};

const discoverMoviePageOne = {
  movieId: 3,
  title: "Hot Fuzz",
  runtimeMinutes: 121,
  genres: ["Comedy", "Action"],
  posterUrl: "https://image.tmdb.org/t/p/w500/hot-fuzz.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/hot-fuzz-backdrop.jpg",
  releaseYear: 2007,
  overview: "A decorated cop gets reassigned to a sleepy village."
};

const discoverMoviePageTwo = {
  movieId: 4,
  title: "Palm Springs",
  runtimeMinutes: 90,
  genres: ["Comedy", "Romance"],
  posterUrl: "https://image.tmdb.org/t/p/w500/palm-springs.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/palm-springs-backdrop.jpg",
  releaseYear: 2020,
  overview: "Two wedding guests get stuck in a time loop."
};

const searchMovie = {
  movieId: 5,
  title: "Arrival",
  runtimeMinutes: 116,
  genres: ["Science Fiction", "Drama"],
  posterUrl: "https://image.tmdb.org/t/p/w500/arrival.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/arrival-backdrop.jpg",
  releaseYear: 2016,
  overview: "A linguist works to understand a new visitor."
};

const fetchMock = vi.fn<typeof fetch>();

describe("App", () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
    fetchMock.mockReset();
  });

  it("does not reset the panel when the active movie is selected again", async () => {
    fetchMock.mockImplementation(async (input) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/movies/1?countryCode=NL")) {
        return createJsonResponse(detailsMovieNl);
      }

      if (url.endsWith("/api/movies/1/similar")) {
        return createJsonResponse({ items: [] });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    const selectMovieButton = await screen.findByRole("button", { name: "View details for Arrival" });
    await userEvent.click(selectMovieButton);

    await screen.findByText("Where to watch in Netherlands (NL)");
    expect(screen.getByText("Videoland")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(4);

    await userEvent.click(selectMovieButton);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(4);
    });

    expect(screen.queryByText("Loading movie details...")).not.toBeInTheDocument();
    expect(screen.getByText("Apple TV")).toBeInTheDocument();
  });

  it("reloads provider availability when the watch region changes", async () => {
    fetchMock.mockImplementation(async (input) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/movies/1?countryCode=NL")) {
        return createJsonResponse(detailsMovieNl);
      }

      if (url.endsWith("/api/movies/1?countryCode=US")) {
        return createJsonResponse(detailsMovieUs);
      }

      if (url.endsWith("/api/movies/1/similar")) {
        return createJsonResponse({ items: [] });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await userEvent.click(await screen.findByRole("button", { name: "View details for Arrival" }));
    await screen.findByText("Videoland");

    await userEvent.selectOptions(screen.getByLabelText("Watch region"), "US");

    await screen.findByText("Where to watch in United States (US)");
    await screen.findByText("Netflix");
    expect(screen.queryByText("Videoland")).not.toBeInTheDocument();

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(5);
    });
  });

  it("submits the recommendation form and renders recommendation results", async () => {
    fetchMock.mockImplementation(async (input, init) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/recommendations")) {
        expect(init?.method).toBe("POST");
        expect(init?.body).toBe(
          JSON.stringify({
            mood: "FeelGood",
            timeBudgetMinutes: 95,
            query: "time loop",
            avoidGenres: ["Horror"],
            releaseYear: 2020,
            countryCode: "US"
          })
        );

        return createJsonResponse({ items: [recommendedMovie] });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await screen.findByRole("button", { name: "Avoid genre Horror" });

    await userEvent.selectOptions(screen.getByLabelText("Watch region"), "US");
    await userEvent.type(screen.getByLabelText("Release year"), "2020");
    await userEvent.clear(screen.getByLabelText("Time budget (minutes)"));
    await userEvent.type(screen.getByLabelText("Time budget (minutes)"), "95");
    await userEvent.type(screen.getByLabelText("Optional hint"), "time loop");
    await userEvent.click(screen.getByRole("button", { name: "Avoid genre Horror" }));
    await userEvent.click(screen.getByRole("button", { name: "Get recommendations" }));

    await screen.findByRole("heading", { name: "Recommendation Results" });
    expect(screen.getByText("Palm Springs")).toBeInTheDocument();
    expect(screen.getByText("Matches the feel-good mood.")).toBeInTheDocument();
    expect(screen.getByText("Prime Video")).toBeInTheDocument();
  });

  it("hides stale recommendation results when the shared release year changes", async () => {
    fetchMock.mockImplementation(async (input, init) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/recommendations")) {
        expect(init?.method).toBe("POST");
        return createJsonResponse({ items: [recommendedMovie] });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await screen.findByRole("button", { name: "Avoid genre Horror" });

    await userEvent.type(screen.getByLabelText("Release year"), "2020");
    await userEvent.click(screen.getByRole("button", { name: "Get recommendations" }));

    await screen.findByRole("heading", { name: "Recommendation Results" });
    expect(screen.getByText("Palm Springs")).toBeInTheDocument();

    const releaseYearInput = screen.getByLabelText("Release year");
    await userEvent.clear(releaseYearInput);
    await userEvent.type(releaseYearInput, "2021");

    expect(screen.queryByRole("heading", { name: "Recommendation Results" })).not.toBeInTheDocument();
    expect(screen.queryByText("Palm Springs")).not.toBeInTheDocument();

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(3);
    });
  });

  it("loads genre discovery results and paginates them", async () => {
    fetchMock.mockImplementation(async (input) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/movies/discover?genre=Comedy&page=1&pageSize=12&releaseYear=2007")) {
        return createJsonResponse({
          items: [discoverMoviePageOne],
          page: 1,
          pageSize: 12,
          totalResults: 13,
          totalPages: 2,
          hasNextPage: true
        });
      }

      if (url.endsWith("/api/movies/discover?genre=Comedy&page=2&pageSize=12&releaseYear=2007")) {
        return createJsonResponse({
          items: [discoverMoviePageTwo],
          page: 2,
          pageSize: 12,
          totalResults: 13,
          totalPages: 2,
          hasNextPage: false
        });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await userEvent.type(screen.getByLabelText("Release year"), "2007");
    await userEvent.click(await screen.findByRole("button", { name: "Browse genre Comedy" }));

    await screen.findByRole("heading", { name: "Comedy Picks" });
    expect(screen.getByText("Hot Fuzz")).toBeInTheDocument();
    expect(screen.getByText("13 results from 2007. Select a card for details and similar titles.")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Next" }));

    await screen.findByText("Palm Springs");

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(4);
    });
  });

  it("includes the release year filter when searching", async () => {
    fetchMock.mockImplementation(async (input) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/movies/search?query=Arrival&page=1&pageSize=12&releaseYear=2016")) {
        return createJsonResponse({
          items: [searchMovie],
          page: 1,
          pageSize: 12,
          totalResults: 1,
          totalPages: 1,
          hasNextPage: false
        });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await screen.findByLabelText("Search movies");

    await userEvent.type(screen.getByLabelText("Search movies"), "Arrival");
    await userEvent.type(screen.getByLabelText("Release year"), "2016");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    const searchResultsHeading = await screen.findByRole("heading", { name: "Search Results" });
    expect(screen.getByRole("heading", { name: "Arrival" })).toBeInTheDocument();
    const searchSummary = searchResultsHeading.parentElement?.querySelector("p");
    expect(searchSummary?.textContent).toContain("(1 results in 2016). Select a card for deeper context.");
  });

  it("uses the input as the single source of truth when the release year is invalid", async () => {
    fetchMock.mockImplementation(async (input) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
      }

      if (url.endsWith("/api/genres")) {
        return createJsonResponse({ items: ["Comedy", "Horror", "Drama"] });
      }

      if (url.endsWith("/api/movies/search?query=Arrival&page=1&pageSize=12&releaseYear=2016")) {
        return createJsonResponse({
          items: [searchMovie],
          page: 1,
          pageSize: 12,
          totalResults: 1,
          totalPages: 1,
          hasNextPage: false
        });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await screen.findByLabelText("Search movies");

    await userEvent.type(screen.getByLabelText("Search movies"), "Arrival");
    await userEvent.type(screen.getByLabelText("Release year"), "2016");
    await userEvent.click(screen.getByRole("button", { name: "Search" }));

    await screen.findByRole("heading", { name: "Search Results" });

    const releaseYearInput = screen.getByLabelText("Release year");
    await userEvent.click(releaseYearInput);
    await userEvent.type(releaseYearInput, "{backspace}{backspace}");

    const searchResultsHeading = screen.getByRole("heading", { name: "Search Results" });
    const searchSummary = searchResultsHeading.parentElement?.querySelector("p");

    expect(screen.getByText("Use a four-digit release year.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Search" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Get recommendations" })).toBeDisabled();
    expect(searchSummary?.textContent).toContain("Fix the release year to refresh results.");
    expect(screen.getByText(/Release year:/).textContent).toContain("Fix input.");

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(3);
    });
  });
});

function createJsonResponse(body: unknown): Response {
  return {
    ok: true,
    json: async () => body
  } as Response;
}
