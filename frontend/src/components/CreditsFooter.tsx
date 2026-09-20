const tmdbLogoUrl =
  "https://www.themoviedb.org/assets/2/v4/logos/v2/blue_square_2-d537fb228cf3ded904ef09b136fe3fec72548ebc1fea3fbbd1ad9e36364db38b.svg";

export function CreditsFooter({ catalogMode }: { catalogMode?: "demo" | "tmdb" }) {
  if (catalogMode !== "tmdb") {
    return (
      <footer className="credits">
        <p>{catalogMode === "demo"
          ? "Demo films, artwork, and provider names are fictional examples created for Watch Compass. Live mode uses TMDB."
          : "Catalog source has not been verified."}</p>
      </footer>
    );
  }
  return (
    <footer className="credits" aria-labelledby="credits-heading">
      <div>
        <p className="credits-label" id="credits-heading">Data credits</p>
        <p>This product uses the TMDB API but is not endorsed or certified by TMDB.</p>
      </div>
      <a href="https://www.themoviedb.org" target="_blank" rel="noreferrer" aria-label="Visit The Movie Database">
        <img src={tmdbLogoUrl} alt="The Movie Database (TMDB)" />
      </a>
    </footer>
  );
}
