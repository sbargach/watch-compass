# Watch Compass

## Running locally
- Prerequisite: .NET SDK 10.0.100 (see `global.json`).
- Set TMDB API key (v4 bearer token) via env var: `set TMDB__ApiKey=<your_key>` (PowerShell) or `export TMDB__ApiKey=<your_key>` (bash). Optional overrides: `TMDB__BaseUrl`, `TMDB__DefaultCountryCode`, `TMDB__Language`, `TMDB__RequestTimeoutSeconds`, `TMDB__MaxRetries`, `TMDB__BackoffBaseMilliseconds`, `TMDB__BackoffJitterMilliseconds`.
- Restore/build: `dotnet build`.
- Run the API: `dotnet run --project src/WatchCompass.Api`.

## Tests
- Deterministic HTTP via WireMock; no live TMDB calls.
- Run everything: `dotnet test`.
- Integration-only example: `dotnet test tests/WatchCompass.IntegrationTests`.

## Architecture
- Contracts: request/response DTOs for the HTTP surface.
- Application: use-cases and abstractions (`IMovieCatalog`) without infrastructure concerns.
- Infrastructure: TMDB-backed `IMovieCatalog` using `HttpClientFactory`, options, retries, and provider mapping.
- API: ASP.NET Core entrypoint, DI wiring, controllers, middleware, and telemetry.
