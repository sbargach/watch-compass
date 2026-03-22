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

const detailsMovie = {
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

      if (url.endsWith("/api/movies/1")) {
        return createJsonResponse(detailsMovie);
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

    await screen.findByText("Where to watch");
    expect(screen.getByText("Netflix")).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(4);

    await userEvent.click(selectMovieButton);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(4);
    });

    expect(screen.queryByText("Loading movie details...")).not.toBeInTheDocument();
    expect(screen.getByText("Apple TV")).toBeInTheDocument();
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
            countryCode: "NL"
          })
        );

        return createJsonResponse({ items: [recommendedMovie] });
      }

      throw new Error(`Unexpected fetch call: ${url}`);
    });

    vi.stubGlobal("fetch", fetchMock);

    render(<App />);

    await screen.findByRole("button", { name: "Horror" });

    await userEvent.clear(screen.getByLabelText("Time budget (minutes)"));
    await userEvent.type(screen.getByLabelText("Time budget (minutes)"), "95");
    await userEvent.type(screen.getByLabelText("Optional hint"), "time loop");
    await userEvent.click(screen.getByRole("button", { name: "Horror" }));
    await userEvent.click(screen.getByRole("button", { name: "Get recommendations" }));

    await screen.findByRole("heading", { name: "Recommendation Results" });
    expect(screen.getByText("Palm Springs")).toBeInTheDocument();
    expect(screen.getByText("Matches the feel-good mood.")).toBeInTheDocument();
    expect(screen.getByText("Prime Video")).toBeInTheDocument();
  });
});

function createJsonResponse(body: unknown): Response {
  return {
    ok: true,
    json: async () => body
  } as Response;
}
