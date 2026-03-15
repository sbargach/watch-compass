import { getApiBaseUrl, requestJson } from "./http";
import type { SearchMoviesResponse, TrendingMoviesResponse } from "../types/movies";

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
