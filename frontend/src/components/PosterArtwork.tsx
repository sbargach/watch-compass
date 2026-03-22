import { useState } from "react";

type PosterArtworkProps = {
  title: string;
  posterUrl: string | null;
  className?: string;
};

export function PosterArtwork({ title, posterUrl, className }: PosterArtworkProps) {
  const [hasError, setHasError] = useState(false);

  if (!posterUrl || hasError) {
    return (
      <div className={className ? `${className} poster-fallback` : "poster-fallback"} aria-hidden="true">
        {title.slice(0, 1).toUpperCase()}
      </div>
    );
  }

  return (
    <img
      className={className}
      src={posterUrl}
      alt={`${title} poster`}
      loading="lazy"
      onError={() => setHasError(true)}
    />
  );
}
