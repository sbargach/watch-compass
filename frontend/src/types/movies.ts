export interface MovieCard {
  movieId: number;
  title: string;
  runtimeMinutes: number | null;
  genres: string[];
  posterUrl: string | null;
  backdropUrl: string | null;
  releaseYear: number | null;
  overview: string | null;
}

export interface MovieDetails extends MovieCard {
  providers: string[];
}

export type Mood = "Chill" | "FeelGood" | "Intense" | "Scary";

export interface Recommendation extends MovieCard {
  providers: string[];
  reasons: string[];
}

export interface RecommendationsRequest {
  mood: Mood;
  timeBudgetMinutes: number;
  query?: string;
  avoidGenres: string[];
  countryCode: string;
}

export interface TrendingMoviesResponse {
  items: MovieCard[];
}

export interface SimilarMoviesResponse {
  items: MovieCard[];
}

export interface GenresResponse {
  items: string[];
}

export interface RecommendationsResponse {
  items: Recommendation[];
}

export interface SearchMoviesResponse {
  items: MovieCard[];
  page: number;
  pageSize: number;
  totalResults: number;
  totalPages: number;
  hasNextPage: boolean;
}
