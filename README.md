# Watch Compass

## Running locally
- Prerequisites: .NET SDK 10.0.100 and Node.js 24+.
- Set TMDB API key (v4 bearer token) via env var: `$env:TMDB__ApiKey="<your_key>"` (PowerShell) or `export TMDB__ApiKey=<your_key>` (bash). Optional overrides: `TMDB__BaseUrl`, `TMDB__DefaultCountryCode`, `TMDB__Language`, `TMDB__RequestTimeoutSeconds`, `TMDB__MaxRetries`, `TMDB__BackoffBaseMilliseconds`, `TMDB__BackoffJitterMilliseconds`.
- Restore/build: `dotnet build`.
- Run the API: `dotnet run --project src/WatchCompass.Api`.
- Frontend setup: `cd frontend && npm install`.
- Run the frontend: `cd frontend && npm run dev`.
- Optional frontend API override: set `VITE_API_BASE_URL` (defaults to `http://localhost:5276`).
- CORS origins for local frontend are configured in `src/WatchCompass.Api/appsettings.Development.json` (`Cors:AllowedOrigins`).

## Tests
- Deterministic HTTP via WireMock; no live TMDB calls.
- Run everything: `dotnet test`.
- Integration-only example: `dotnet test tests/WatchCompass.IntegrationTests`.

## Architecture
- Contracts: request/response DTOs for the HTTP surface.
- Application: use-cases and abstractions (`IMovieCatalog`) without infrastructure concerns.
- Infrastructure: TMDB-backed `IMovieCatalog` using `HttpClientFactory`, options, retries, and provider mapping.
- API: ASP.NET Core entrypoint, DI wiring, controllers, middleware, and telemetry.
- Caching: in-memory decorator on TMDB catalog (configurable via `Caching:MovieCatalog`).
- Frontend: React + TypeScript + Vite client (`frontend/`) consuming trending, paginated search, movie details, and similar-title endpoints.

## API surface
- `GET /api/movies/search?query=...&page=1&pageSize=10` - find movies by query with paged results (`pageSize` max 50).
- `GET /api/movies/trending?limit=...` - fetch today's trending movies (limit defaults to 10, max 50).
- `GET /api/movies/{movieId}?countryCode=XX` - fetch details and available providers for a movie.
- `GET /api/movies/{movieId}/similar` - fetch similar movies for a given title.
- `POST /api/recommendations` - generate recommendations based on mood, time budget, and optional query/avoids.
- Swagger UI available at `/swagger` (XML comments enabled).
