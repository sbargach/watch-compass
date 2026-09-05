import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CompassPickPanel } from "./CompassPickPanel";
import type { MovieCard } from "../types/movies";

const arrival: MovieCard = {
  movieId: 1,
  title: "Arrival",
  runtimeMinutes: 116,
  genres: ["Science Fiction", "Drama"],
  posterUrl: "https://image.tmdb.org/t/p/w500/arrival.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/arrival-backdrop.jpg",
  releaseYear: 2016,
  overview: "A linguist works to understand a new visitor."
};

const palmSprings: MovieCard = {
  movieId: 2,
  title: "Palm Springs",
  runtimeMinutes: 90,
  genres: ["Comedy", "Romance"],
  posterUrl: "https://image.tmdb.org/t/p/w500/palm-springs.jpg",
  backdropUrl: "https://image.tmdb.org/t/p/original/palm-springs-backdrop.jpg",
  releaseYear: 2020,
  overview: "Two wedding guests get stuck in a time loop."
};

describe("CompassPickPanel", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it("spins before selecting a random movie", () => {
    const onSelectMovie = vi.fn();
    vi.spyOn(Math, "random")
      .mockReturnValueOnce(0.7)
      .mockReturnValueOnce(0.25);

    render(
      <CompassPickPanel
        movies={[arrival, palmSprings]}
        isCatalogLoading={false}
        hasCatalogError={false}
        onSelectMovie={onSelectMovie}
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Spin for a pick" }));

    expect(screen.getByRole("button", { name: "Spinning..." })).toBeDisabled();
    expect(screen.getByText("Finding a pick...")).toBeInTheDocument();
    expect(onSelectMovie).not.toHaveBeenCalled();

    act(() => {
      vi.advanceTimersByTime(950);
    });

    expect(onSelectMovie).toHaveBeenCalledWith(palmSprings);
    expect(screen.getByText("Compass picked Palm Springs.")).toBeInTheDocument();
  });

  it("keeps random picking disabled until movies are available", () => {
    render(
      <CompassPickPanel
        movies={[]}
        isCatalogLoading={false}
        hasCatalogError={true}
        onSelectMovie={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: "Spin for a pick" })).toBeDisabled();
    expect(screen.getByText("Connect the API or retry a feed to unlock random picks.")).toBeInTheDocument();
  });
});
