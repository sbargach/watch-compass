import { postJson, requestJson } from "./http";
import type {
  GenresResponse,
  MovieDetails,
  NowPlayingMoviesResponse,
  RecommendationsRequest,
  RecommendationsResponse,
  SearchMoviesResponse,
  SimilarMoviesResponse,
  TrendingMoviesResponse
} from "../types/movies";

export async function getTrendingMovies(limit: number, signal?: AbortSignal): Promise<TrendingMoviesResponse> {
  return requestJson<TrendingMoviesResponse>(`/api/movies/trending?limit=${limit}`, signal);
}

export async function getNowPlayingMovies(limit: number, signal?: AbortSignal): Promise<NowPlayingMoviesResponse> {
  return requestJson<NowPlayingMoviesResponse>(`/api/movies/now-playing?limit=${limit}`, signal);
}

export async function searchMovies(
  query: string,
  page: number,
  pageSize: number,
  releaseYear?: number,
  signal?: AbortSignal
): Promise<SearchMoviesResponse> {
  const params = new URLSearchParams({
    query,
    page: String(page),
    pageSize: String(pageSize)
  });
  if (releaseYear !== undefined) {
    params.set("releaseYear", String(releaseYear));
  }

  return requestJson<SearchMoviesResponse>(`/api/movies/search?${params.toString()}`, signal);
}

export async function discoverMovies(
  genre: string,
  page: number,
  pageSize: number,
  releaseYear?: number,
  signal?: AbortSignal
): Promise<SearchMoviesResponse> {
  const params = new URLSearchParams({
    genre,
    page: String(page),
    pageSize: String(pageSize)
  });
  if (releaseYear !== undefined) {
    params.set("releaseYear", String(releaseYear));
  }

  return requestJson<SearchMoviesResponse>(`/api/movies/discover?${params.toString()}`, signal);
}

export async function getMovieDetails(
  movieId: number,
  countryCode?: string,
  signal?: AbortSignal
): Promise<MovieDetails> {
  const params = new URLSearchParams();
  if (countryCode && countryCode.trim().length > 0) {
    params.set("countryCode", countryCode.trim());
  }

  const queryString = params.toString();
  const query = queryString.length > 0 ? `?${queryString}` : "";
  return requestJson<MovieDetails>(`/api/movies/${movieId}${query}`, signal);
}

export async function getSimilarMovies(movieId: number, signal?: AbortSignal): Promise<SimilarMoviesResponse> {
  return requestJson<SimilarMoviesResponse>(`/api/movies/${movieId}/similar`, signal);
}

export async function getGenres(signal?: AbortSignal): Promise<GenresResponse> {
  return requestJson<GenresResponse>("/api/genres", signal);
}

export async function getRecommendations(
  request: RecommendationsRequest,
  signal?: AbortSignal
): Promise<RecommendationsResponse> {
  return postJson<RecommendationsResponse>("/api/recommendations", request, signal);
}
