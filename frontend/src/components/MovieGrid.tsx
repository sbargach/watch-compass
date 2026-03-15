import { MovieTile } from "./MovieTile";
import type { MovieCard } from "../types/movies";

type MovieGridProps = {
  movies: MovieCard[];
};

export function MovieGrid({ movies }: MovieGridProps) {
  return (
    <div className="movie-grid">
      {movies.map((movie) => (
        <MovieTile key={movie.movieId} movie={movie} />
      ))}
    </div>
  );
}
