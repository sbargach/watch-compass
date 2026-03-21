import { render, screen, waitFor } from "@testing-library/react";
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

const fetchMock = vi.fn<typeof fetch>();

describe("App", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    fetchMock.mockReset();
  });

  it("does not reset the panel when the active movie is selected again", async () => {
    fetchMock.mockImplementation(async (input) => {
      const url = String(input);

      if (url.endsWith("/api/movies/trending?limit=12")) {
        return createJsonResponse({ items: [trendingMovie] });
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
    expect(fetchMock).toHaveBeenCalledTimes(3);

    await userEvent.click(selectMovieButton);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(3);
    });

    expect(screen.queryByText("Loading movie details...")).not.toBeInTheDocument();
    expect(screen.getByText("Apple TV")).toBeInTheDocument();
  });
});

function createJsonResponse(body: unknown): Response {
  return {
    ok: true,
    json: async () => body
  } as Response;
}
