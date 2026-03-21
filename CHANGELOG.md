# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.13.0] - 2026-03-21
### Added
- Selectable movie cards in the frontend, opening a rich details panel backed by `GET /api/movies/{movieId}`.
- Similar movie exploration in the details panel, wired to `GET /api/movies/{movieId}/similar`.
- Provider badges, backdrop treatment, and active-card states to make the search and trending flows feel connected.
- Frontend regression coverage for the active-movie selection flow to protect the details panel state machine.

### Fixed
- Re-selecting the active movie no longer resets the details panel into a loading state without issuing a new request.
- Selectable movie tiles now use valid HTML structure with a dedicated overlay button instead of block content nested inside a button.

### Changed
- Bumped project package versions from `0.12.0` to `0.13.0` across Domain, Contracts, Application, Infrastructure, API, and frontend packages.

## [0.12.0] - 2026-03-15
### Added
- Frontend app scaffold (`frontend/`) using React + TypeScript + Vite.
- Initial UI for trending movies and paginated movie search, wired to `/api/movies/trending` and `/api/movies/search`.
- Typed frontend API client and reusable UI components for movie cards and pagination.

### Changed
- Added configurable API CORS policy with development origins for local frontend integration.
- Updated project documentation with frontend setup/run instructions.
- Bumped project package versions from `0.11.0` to `0.12.0` across Domain, Contracts, Application, Infrastructure, and API projects.

## [0.11.0] - 2026-03-08
### Added
- Paginated movie search on `GET /api/movies/search` with `page` and `pageSize` query parameters.
- Search response metadata (`page`, `pageSize`, `totalResults`, `totalPages`, `hasNextPage`) for frontend pagination flows.
- TMDB-backed paged search mapping with support for slicing across TMDB page boundaries.

### Changed
- Search cache keys now include query + page + page size to keep pagination results deterministic.
- Expanded unit and integration coverage for paginated search behavior and validation.

## [0.10.0] - 2026-02-07
### Added
- Trending movies endpoint (`GET /api/movies/trending`) with configurable result limit and reuse of card metadata.
- TMDB client/catalog support for daily trending titles plus in-memory caching and unit/integration coverage.
- Configurable cache duration for trending results (defaults to 15 minutes).

## [0.9.1] - 2026-02-03
### Changed
- TMDB options now require an API key during startup validation to fail fast on misconfiguration.
- Added unit coverage for the TMDB options validator.

## [0.9.0] - 2026-01-25
### Added
- Similar movies surfaced via TMDB `/movie/{id}/similar` and exposed through new `/api/movies/{movieId}/similar` endpoint.
- Caching support for similar results plus configurable cache durations for similar and genres.
- Contracts, use cases, and integration coverage for similar movie responses.

## [0.8.0] - 2026-01-18
### Added
- TMDB genre list exposed via new `/api/genres` endpoint and plumbed through catalog with caching.
- Genre IDs in TMDB search results are resolved to names for API responses and avoid-genre filtering.

## [0.7.0] - 2026-01-17
### Added
- TMDB mapping now surfaces poster/backdrop URLs, release year, and overview across search, details, and recommendations.
- API contracts expose the richer metadata for UI cards while keeping provider lists intact.

## [0.6.0] - 2026-01-10
### Added
- In-memory caching decorator for TMDB catalog (configurable durations).
- Movie details endpoint including providers plus integration + unit coverage.
- CI workflow (build/test) and Swagger XML docs wiring.

## [0.5.0] - 2026-01-07
### Changed
- Reworked TMDB integration to use a typed HttpClient + request executor with configurable retries, timeouts, and backoff.
- Unified TMDB configuration validation (ApiKey now throws a dedicated configuration exception).
- Added WireMock-backed integration tests covering TMDB mapping and API provider population without live HTTP.

## [0.4.0] - 2025-01-04
- Added: recommendation engine v1 with explainable reasons
- Added: request validation and consistent 400 ProblemDetails
- Added: unit + integration tests for recommendations

## [0.3.0] - 2026-01-03
### Added
- Added: TMDB catalog via Refit (search/details/providers)
- Added: OpenTelemetry tracing + Prometheus metrics (/metrics)
- Added: TMDB mapping unit tests with fixtures

## [0.2.0] - 2025-12-28
### Added
- Backend scaffold with contracts, minimal API endpoints, Serilog, and NUnit/Shouldly test suites
### Changed
- Centralized JSON serialization for responses and problem payloads
- Hardened exception handling to avoid leaking internal details
- Documented contract DTO properties for clearer integration

## [0.1.0] - 2025-12-27
### Added
- Add project scaffold and folder structure.
