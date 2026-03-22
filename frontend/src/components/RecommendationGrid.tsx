import type { Recommendation } from "../types/movies";
import { PosterArtwork } from "./PosterArtwork";

type RecommendationGridProps = {
  recommendations: Recommendation[];
  selectedMovieId?: number | null;
  onSelectRecommendation: (recommendation: Recommendation) => void;
};

export function RecommendationGrid({
  recommendations,
  selectedMovieId = null,
  onSelectRecommendation
}: RecommendationGridProps) {
  return (
    <div className="recommendation-grid">
      {recommendations.map((recommendation) => {
        const meta = buildMeta(recommendation);
        const isActive = selectedMovieId === recommendation.movieId;

        return (
          <article
            key={recommendation.movieId}
            className={`recommendation-card${isActive ? " recommendation-card-active" : ""}`}
          >
            <button
              type="button"
              className="recommendation-card-button"
              aria-label={`View details for ${recommendation.title}`}
              aria-pressed={isActive}
              onClick={() => onSelectRecommendation(recommendation)}
            />

            <div className="recommendation-card-poster">
              <PosterArtwork title={recommendation.title} posterUrl={recommendation.posterUrl} />
            </div>

            <div className="recommendation-card-content">
              <div className="recommendation-card-header">
                <div>
                  <h3>{recommendation.title}</h3>
                  <p className="movie-meta">{meta}</p>
                </div>

                {recommendation.providers.length > 0 && (
                  <div className="recommendation-chip-list recommendation-chip-list-compact">
                    {recommendation.providers.map((provider) => (
                      <span key={provider} className="recommendation-chip">
                        {provider}
                      </span>
                    ))}
                  </div>
                )}
              </div>

              <p className="movie-overview recommendation-overview">
                {recommendation.overview?.trim() || "No overview available yet."}
              </p>

              <div className="recommendation-group recommendation-group-highlight">
                <p className="recommendation-label">Why it fits</p>
                <ul className="recommendation-list">
                  {recommendation.reasons.map((reason) => (
                    <li key={reason}>{reason}</li>
                  ))}
                </ul>
              </div>
            </div>
          </article>
        );
      })}
    </div>
  );
}

function buildMeta(recommendation: Recommendation): string {
  const meta: string[] = [];

  if (recommendation.releaseYear) {
    meta.push(String(recommendation.releaseYear));
  }

  if (recommendation.runtimeMinutes) {
    meta.push(`${recommendation.runtimeMinutes} min`);
  }

  return meta.length > 0 ? meta.join(" | ") : "Runtime unavailable";
}
