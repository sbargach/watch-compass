import { useEffect, useState } from "react";
import { discoverMovies, searchMovies } from "../../api/moviesApi";
import type { SearchMoviesResponse } from "../../types/movies";

export type PagedMoviesState = {
  result: SearchMoviesResponse | null;
  isLoading: boolean;
  error: string | null;
};

type Options = {
  mode: "search" | "discover";
  value: string | null;
  page: number;
  pageSize: number;
  releaseYear?: number;
  isValid: boolean;
  reloadKey?: number;
};

const emptyState: PagedMoviesState = { result: null, isLoading: false, error: null };
type StoredState = { key: string; result: SearchMoviesResponse | null; error: string | null };

export function usePagedMovies({
  mode,
  value,
  page,
  pageSize,
  releaseYear,
  isValid,
  reloadKey = 0
}: Options): PagedMoviesState {
  const [state, setState] = useState<StoredState>({ key: "", result: null, error: null });
  const requestKey = `${mode}:${value ?? ""}:${page}:${pageSize}:${releaseYear ?? "all"}:${reloadKey}`;

  useEffect(() => {
    if (value === null || value.length === 0) {
      return;
    }
    if (!isValid) {
      return;
    }

    const controller = new AbortController();
    const request = mode === "search"
      ? searchMovies(value, page, pageSize, releaseYear, controller.signal)
      : discoverMovies(value, page, pageSize, releaseYear, controller.signal);
    void request
      .then((result) => {
        if (!controller.signal.aborted) {
          setState({ key: requestKey, result, error: null });
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState({ key: requestKey, result: null, error: toErrorMessage(error) });
        }
      });

    return () => controller.abort();
  }, [isValid, mode, page, pageSize, releaseYear, reloadKey, requestKey, value]);

  if (value === null || value.length === 0) {
    return emptyState;
  }
  if (!isValid) {
    return { result: state.result, isLoading: false, error: state.error };
  }
  if (state.key !== requestKey) {
    return { result: null, isLoading: true, error: null };
  }
  return { result: state.result, isLoading: false, error: state.error };
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "An unexpected error occurred.";
}
