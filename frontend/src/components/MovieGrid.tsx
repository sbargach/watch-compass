import { MovieTile } from "./MovieTile";
import type { MovieCard } from "../types/movies";

type MovieGridProps = {
  movies: MovieCard[];
  onSelectMovie?: (movie: MovieCard) => void;
  selectedMovieId?: number | null;
};

export function MovieGrid({ movies, onSelectMovie, selectedMovieId = null }: MovieGridProps) {
  return (
    <div className="movie-grid">
      {movies.map((movie) => (
        <MovieTile
          key={movie.movieId}
          movie={movie}
          onSelect={onSelectMovie}
          isActive={selectedMovieId === movie.movieId}
        />
      ))}
    </div>
  );
}
