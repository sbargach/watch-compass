import { useCallback, useEffect, useState } from "react";
import { getGenres, getNowPlayingMovies, getTrendingMovies } from "../../api/moviesApi";
import type { MovieCard } from "../../types/movies";

type ResourceState<T> = {
  data: T;
  error: string | null;
  version: number;
};

const emptyMovies: MovieCard[] = [];
const emptyGenres: string[] = [];

export function useCatalogOverview(trendingLimit: number, nowPlayingLimit: number) {
  const [trendingReload, setTrendingReload] = useState(0);
  const [nowPlayingReload, setNowPlayingReload] = useState(0);
  const [genresReload, setGenresReload] = useState(0);
  const [trending, setTrending] = useState<ResourceState<MovieCard[]>>({
    data: emptyMovies,
    error: null,
    version: -1
  });
  const [nowPlaying, setNowPlaying] = useState<ResourceState<MovieCard[]>>({
    data: emptyMovies,
    error: null,
    version: -1
  });
  const [genres, setGenres] = useState<ResourceState<string[]>>({
    data: emptyGenres,
    error: null,
    version: -1
  });

  useEffect(() => {
    const controller = new AbortController();
    void getTrendingMovies(trendingLimit, controller.signal)
      .then((response) => {
        if (!controller.signal.aborted) {
          setTrending({ data: response.items, error: null, version: trendingReload });
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setTrending({ data: [], error: toErrorMessage(error), version: trendingReload });
        }
      });
    return () => controller.abort();
  }, [trendingLimit, trendingReload]);

  useEffect(() => {
    const controller = new AbortController();
    void getNowPlayingMovies(nowPlayingLimit, controller.signal)
      .then((response) => {
        if (!controller.signal.aborted) {
          setNowPlaying({ data: response.items, error: null, version: nowPlayingReload });
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setNowPlaying({ data: [], error: toErrorMessage(error), version: nowPlayingReload });
        }
      });
    return () => controller.abort();
  }, [nowPlayingLimit, nowPlayingReload]);

  useEffect(() => {
    const controller = new AbortController();
    void getGenres(controller.signal)
      .then((response) => {
        if (!controller.signal.aborted) {
          setGenres({ data: response.items, error: null, version: genresReload });
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setGenres({ data: [], error: toErrorMessage(error), version: genresReload });
        }
      });
    return () => controller.abort();
  }, [genresReload]);

  return {
    trendingState: {
      items: trending.data,
      isLoading: trending.version !== trendingReload,
      error: trending.version === trendingReload ? trending.error : null
    },
    nowPlayingState: {
      items: nowPlaying.data,
      isLoading: nowPlaying.version !== nowPlayingReload,
      error: nowPlaying.version === nowPlayingReload ? nowPlaying.error : null
    },
    genresState: {
      items: genres.data,
      isLoading: genres.version !== genresReload,
      error: genres.version === genresReload ? genres.error : null
    },
    retryTrending: useCallback(() => setTrendingReload((value) => value + 1), []),
    retryNowPlaying: useCallback(() => setNowPlayingReload((value) => value + 1), []),
    retryGenres: useCallback(() => setGenresReload((value) => value + 1), [])
  };
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : "An unexpected error occurred.";
}
