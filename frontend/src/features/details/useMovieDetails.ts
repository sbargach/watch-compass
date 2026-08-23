import { useCallback, useEffect, useState } from "react";
import { getMovieDetails, getSimilarMovies } from "../../api/moviesApi";
import type { MovieCard, MovieDetails } from "../../types/movies";

export type MovieDetailsState = {
  details: MovieDetails | null;
  similarMovies: MovieCard[];
  isLoading: boolean;
  isSimilarLoading: boolean;
  error: string | null;
  similarError: string | null;
};

type StoredState = {
  movieId: number | null;
  details: MovieDetails | null;
  detailsKey: string;
  detailsError: string | null;
  similarMovies: MovieCard[];
  similarKey: string;
  similarError: string | null;
};

const initialState: StoredState = {
  movieId: null,
  details: null,
  detailsKey: "",
  detailsError: null,
  similarMovies: [],
  similarKey: "",
  similarError: null
};

export function useMovieDetails(movieId: number | null, watchRegion: string): MovieDetailsState & { retry: () => void } {
  const [state, setState] = useState<StoredState>(initialState);
  const [reloadKey, setReloadKey] = useState(0);
  const detailsKey = `${movieId ?? "none"}:${watchRegion}:${reloadKey}`;
  const similarKey = `${movieId ?? "none"}:${reloadKey}`;

  useEffect(() => {
    if (movieId === null) {
      return;
    }

    const controller = new AbortController();
    void getMovieDetails(movieId, watchRegion, controller.signal)
      .then((details) => {
        if (!controller.signal.aborted) {
          setState((current) => ({
            ...current,
            movieId,
            details,
            detailsKey,
            detailsError: null
          }));
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState((current) => ({
            ...current,
            movieId,
            details: null,
            detailsKey,
            detailsError: toErrorMessage(error)
          }));
        }
      });
    return () => controller.abort();
  }, [detailsKey, movieId, watchRegion]);

  useEffect(() => {
    if (movieId === null) {
      return;
    }

    const controller = new AbortController();
    void getSimilarMovies(movieId, controller.signal)
      .then((response) => {
        if (!controller.signal.aborted) {
          setState((current) => ({
            ...current,
            movieId,
            similarMovies: response.items,
            similarKey,
            similarError: null
          }));
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState((current) => ({
            ...current,
            movieId,
            similarMovies: [],
            similarKey,
            similarError: toErrorMessage(error)
          }));
        }
      });
    return () => controller.abort();
  }, [movieId, similarKey]);

  const retry = useCallback(() => setReloadKey((value) => value + 1), []);
  if (movieId === null) {
    return {
      details: null,
      similarMovies: [],
      isLoading: false,
      isSimilarLoading: false,
      error: null,
      similarError: null,
      retry
    };
  }

  const sameMovie = state.movieId === movieId;
  return {
    details: sameMovie ? state.details : null,
    similarMovies: sameMovie ? state.similarMovies : [],
    isLoading: state.detailsKey !== detailsKey,
    isSimilarLoading: state.similarKey !== similarKey,
    error: state.detailsKey === detailsKey ? state.detailsError : null,
    similarError: state.similarKey === similarKey ? state.similarError : null,
    retry
  };
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "An unexpected error occurred.";
}
