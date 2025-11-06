## Blocked Countries – Overview and Guide

This solution provides an API to block traffic by country, look up IP geolocation, record access attempts, and manage country block lists. It uses a layered architecture with structured logging, lightweight rate limiting/caching for outbound geolocation calls, and background jobs for maintenance.

## Solution Structure

- **Presentation (Web API)**: `Blocked Countries`
  - ASP.NET Core Web API exposing endpoints under `/api/*`
  - Swagger UI (development) at `/swagger`
- **Business (Domain/Services)**: `BlockedCountries.Business`
  - Core service logic and models
  - Orchestrates repositories and external calls (geolocation)
- **Data (Persistence/Repositories)**: `BlockedCountries.Data`
  - In‑memory repository implementations and data models
- **Tests**: `BlockedCountries.Tests`
  - xUnit tests for controllers and services

## Cross‑Cutting Features

- **Serilog** (structured logging)
  - Configured via `appsettings.json`
  - Request logging middleware enabled (`UseSerilogRequestLogging`)
  - Writes to console/file (per configuration)
- **Swagger/OpenAPI**
  - Enabled in Development
  - Document title: "Blocked Countries API v1"
- **HTTP Client Pipeline for Geolocation**
  - `IGeolocationService` implemented by `GeolocationService`
  - Outbound client configured with base URL, timeout, default headers
  - Handlers:
    - `GeolocationRateLimitHandler` (simple throttle)
    - `GeolocationCacheHandler` (in‑memory caching)
- **Basic Rate Limiting (service‑level)**
- **Caching**: Memory cache registered and used by `GeolocationCacheHandler`
- **Hangfire Background Jobs**
  - Hangfire configured with `MemoryStorage`
  - Recurring job: `TemporalBlockCleanupJob` every 5 minutes to purge expired temporal blocks
- **Configuration**
  - Strongly typed via `GeolocationApiConfig` (BaseUrl, RateLimitPerMinute, etc.)

## Dependency Injection (selected registrations)

- Repositories: `ICountryRepository`, `IBlockedAttemptRepository`
- Services: `ICountryManagementService`, `IIpBlockingService`, `IBlockedAttemptService`, `IGeolocationService`
- Handlers: `GeolocationRateLimitHandler`, `GeolocationCacheHandler`

## API Endpoints

Base path: `/api`

### Countries
- `POST /api/countries/block`
  - Body: `{ "countryCode": "US" }`
  - 200 OK: `BlockedCountryResponse`
  - 400 BadRequest: validation error
- `DELETE /api/countries/block/{countryCode}`
  - 204 NoContent on success
  - 404 NotFound if country not blocked
- `GET /api/countries/blocked?page=1&pageSize=10&search=term`
  - 200 OK: `PagedResponse<BlockedCountryResponse>`
  - Notes:
    - Filters out expired temporal blocks
    - `search` matches code or name (case‑insensitive)
- `POST /api/countries/temporal-block`
  - Body: `{ "countryCode": "US", "durationMinutes": 30 }`
  - 200 OK: temporal block created
  - 400/409 on invalid or conflicting requests

### IP
- `GET /api/ip/lookup?ipAddress={optional}`
  - If `ipAddress` omitted, server uses `HttpContext.Connection.RemoteIpAddress`
  - 200 OK: `IpLookupResponse`
  - 404 if IP info unavailable; 400 on invalid input
- `GET /api/ip/check-block`
  - Uses client IP; looks up country; checks block list; logs attempt
  - 200 OK: `CheckBlockResponse { ipAddress, countryCode, isBlocked }`

### Logs
- `GET /api/logs/blocked-attempts?page=1&pageSize=10`
  - 200 OK: `PagedResponse<BlockedAttemptResponse>`

## Main Business Services

- `CountryManagementService`
  - Block country permanently
  - Unblock country
  - Add temporal block (expires at a future time)
  - List blocked countries with search/pagination and temporal‑expiry filtering
- `IpBlockingService`
  - Lookup IP geolocation via `IGeolocationService`
  - Check if client IP is blocked; logs attempt with user agent and country
- `GeolocationService`
  - Validates IP format
  - Throttles outbound requests (semaphore + minimal interval)
  - Parses JSON from the remote API
  - Returns simplified `IpLookupResponse` or `null` on error/limit

## Logging

- Serilog configured in `Program.cs` and `appsettings*.json`
- Request logs + custom logs from services (warnings/errors)
- Example outputs in `Logs/` directory when file sink enabled

## Jobs (Hangfire)

- Recurring job `temporal-block-cleanup` every 5 minutes (UTC)
- Storage: in‑memory (for demo/dev). For production, switch to a persistent Hangfire storage provider

## Configuration

`appsettings.json` (simplified):
- `GeolocationApi`:
  - `BaseUrl`: base URL of the geolocation service
  - `RateLimitPerMinute`: used by `GeolocationService`
- `Serilog`: sinks/levels/enrichment

## Build & Run

- Requirements: .NET 8 SDK
- Build: `dotnet build`
- Run API: `dotnet run --project "Blocked Countries/Blocked Countries.csproj"`
- Swagger (Dev): `https://localhost:{port}/swagger`

## Tests

- Test project: `BlockedCountries.Tests`
- Run: `dotnet test "BlockedCountries.Tests/BlockedCountries.Tests.csproj"`
- Coverage (optional): add coverlet or your preferred tool

## Notes & Extensibility

- Replace Hangfire memory storage and in‑memory repositories with persistent stores before production
- Replace `GeolocationService` base URL and add API key headers if required by the provider
- Tighten rate‑limit/caching strategies as needed (e.g., Polly, distributed cache)
- Expand model validation/DTOs and add authorization if exposing publicly


