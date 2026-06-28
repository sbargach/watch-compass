# Watch Compass

Watch Compass is a full-stack movie discovery app for exploring what to watch next. It combines TMDB-backed catalog search, genre discovery, now-playing and trending feeds, movie details, similar-title exploration, provider availability, and transparent mood-based recommendations.

The goal is not to hide behind a polished landing page. The UI exposes the same constraints the API validates: pagination, release-year filtering, watch region, runtime budget, provider availability, and empty/error states.

## Engineering Notes
- Layered backend: contracts, application use cases, domain value objects, infrastructure adapters, and API controllers stay separated.
- Typed HTTP boundary: request/response DTOs live in `WatchCompass.Contracts` and are reused across the API surface.
- Resilient TMDB integration: `HttpClientFactory`, options validation, retry/backoff settings, cancellation tokens, and explicit upstream error handling.
- Cache strategy: in-memory catalog decorator with separate TTLs for search, discovery, details, providers, genres, similar titles, trending, and now-playing feeds.
- Observable service shape: Serilog request logging, `/health`, Prometheus metrics, and OpenTelemetry instrumentation.
- Deterministic tests: WireMock fixtures and frontend fetch mocks avoid live TMDB calls in the test suite.
- UX constraint visibility: the React client keeps region, release year, mood, runtime, and genre constraints visible instead of hiding request state.

## Stack
- Backend: ASP.NET Core on .NET 10, C#, OpenTelemetry, Serilog, Swagger.
- Frontend: React 19, TypeScript, Vite, Vitest, Testing Library.
- Tests: NUnit, Shouldly, WireMock.Net, Vitest.
- External API: TMDB v3.

## Running Locally
Prerequisites:
- .NET SDK `10.0.100`
- Node.js `24+`
- TMDB v4 bearer token

From the repository root:

```powershell
$env:TMDB__ApiKey="<your_tmdb_v4_token>"
dotnet build
dotnet run --project src/WatchCompass.Api
```

In a second terminal:

```powershell
cd frontend
npm install
npm run dev
```

The frontend defaults to `http://localhost:5276` for the API. Override it with:

```powershell
$env:VITE_API_BASE_URL="http://localhost:5276"
```

Swagger UI is available in development at `/swagger`.

## Configuration
Important API settings can be supplied through `appsettings*.json`, environment variables, or user secrets.

| Setting | Purpose |
| --- | --- |
| `TMDB__ApiKey` | Required TMDB v4 bearer token. |
| `TMDB__BaseUrl` | TMDB API base URL. Defaults to `https://api.themoviedb.org/3`. |
| `TMDB__DefaultCountryCode` | Default provider region. Defaults to `US`. |
| `TMDB__Language` | TMDB language parameter. Defaults to `en-US`. |
| `TMDB__RequestTimeoutSeconds` | Per-request timeout. |
| `TMDB__MaxRetries` | Retry count for transient upstream failures. |
| `TMDB__BackoffBaseMilliseconds` | Linear retry backoff base. |
| `TMDB__BackoffJitterMilliseconds` | Random retry jitter ceiling. |
| `Caching__MovieCatalog__*Minutes` | Per-flow cache TTLs for catalog calls. Set a value to `0` to disable that cache slice. |
| `Cors__AllowedOrigins__0` | Allowed frontend origins for local or deployed clients. |

## Quality Gates
Run backend tests:

```powershell
dotnet test
```

Run frontend tests and build:

```powershell
cd frontend
npm test
npm run build
```

The integration tests use local WireMock fixtures, so they do not need a TMDB key and should be stable in CI.

## API Surface
- `GET /health` - lightweight health probe.
- `GET /metrics` - Prometheus scraping endpoint.
- `GET /api/genres` - list supported movie genres.
- `GET /api/movies/search?query=...&page=1&pageSize=10&releaseYear=2016` - paged title search with optional release-year filter.
- `GET /api/movies/discover?genre=Action&page=1&pageSize=12&releaseYear=2022` - paged genre discovery sorted by popularity.
- `GET /api/movies/trending?limit=...` - daily trending feed, max `20`.
- `GET /api/movies/now-playing?limit=...` - current theatrical feed, max `20`.
- `GET /api/movies/{movieId}?countryCode=XX` - movie details plus provider availability.
- `GET /api/movies/{movieId}/similar` - similar titles for a selected movie.
- `POST /api/recommendations` - mood, runtime, region, release-year, query, and avoid-genre based shortlist.

## Architecture
- `src/WatchCompass.Contracts` - public DTOs for API requests and responses.
- `src/WatchCompass.Domain` - small domain types such as `Mood` and `TimeBudget`.
- `src/WatchCompass.Application` - use cases and `IMovieCatalog` abstraction.
- `src/WatchCompass.Infrastructure` - TMDB client, request executor, cache decorator, and options validation.
- `src/WatchCompass.Api` - ASP.NET Core entrypoint, controllers, middleware, CORS, Swagger, logging, health, and telemetry.
- `frontend` - React/Vite client for browse, recommendations, details, and provider flows.
- `tests` - unit and integration coverage with fixture-backed HTTP behavior.

## Deliberate Tradeoffs
- Recommendations are transparent heuristics, not ML. This keeps reasons inspectable and testable.
- Cache is in-memory. That is appropriate for local and single-instance deployments, but a multi-instance deployment would need distributed cache or cache-aside storage.
- Authentication is out of scope. The project focuses on catalog integration, service boundaries, and UX state handling.
- TMDB is the only catalog provider. The `IMovieCatalog` abstraction keeps a second provider possible without changing application use cases.
