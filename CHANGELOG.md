# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
