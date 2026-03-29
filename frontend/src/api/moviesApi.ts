import { getApiBaseUrl, postJson, requestJson } from "./http";
import type {
  GenresResponse,
  MovieDetails,
  RecommendationsRequest,
  RecommendationsResponse,
  SearchMoviesResponse,
  SimilarMoviesResponse,
  TrendingMoviesResponse
} from "../types/movies";

export { getApiBaseUrl };

export async function getTrendingMovies(limit: number): Promise<TrendingMoviesResponse> {
  return requestJson<TrendingMoviesResponse>(`/api/movies/trending?limit=${limit}`);
}

export async function searchMovies(
  query: string,
  page: number,
  pageSize: number
): Promise<SearchMoviesResponse> {
  const params = new URLSearchParams({
    query,
    page: String(page),
    pageSize: String(pageSize)
  });

  return requestJson<SearchMoviesResponse>(`/api/movies/search?${params.toString()}`);
}

export async function discoverMovies(
  genre: string,
  page: number,
  pageSize: number
): Promise<SearchMoviesResponse> {
  const params = new URLSearchParams({
    genre,
    page: String(page),
    pageSize: String(pageSize)
  });

  return requestJson<SearchMoviesResponse>(`/api/movies/discover?${params.toString()}`);
}

export async function getMovieDetails(
  movieId: number,
  countryCode?: string
): Promise<MovieDetails> {
  const params = new URLSearchParams();
  if (countryCode && countryCode.trim().length > 0) {
    params.set("countryCode", countryCode.trim());
  }

  const queryString = params.toString();
  const query = queryString.length > 0 ? `?${queryString}` : "";
  return requestJson<MovieDetails>(`/api/movies/${movieId}${query}`);
}

export async function getSimilarMovies(movieId: number): Promise<SimilarMoviesResponse> {
  return requestJson<SimilarMoviesResponse>(`/api/movies/${movieId}/similar`);
}

export async function getGenres(): Promise<GenresResponse> {
  return requestJson<GenresResponse>("/api/genres");
}

export async function getRecommendations(
  request: RecommendationsRequest
): Promise<RecommendationsResponse> {
  return postJson<RecommendationsResponse>("/api/recommendations", request);
}
