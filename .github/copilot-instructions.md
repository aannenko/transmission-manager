# Copilot Instructions for Transmission Manager

## Build, Test, and Lint

This is a .NET 10 solution using central package management (`Directory.Packages.props`). There are three `.slnx` files — use `TransmissionManager.slnx` for full-repo operations, or the scoped ones (`TransmissionManager.Api.slnx`, `TransmissionManager.Web.slnx`) when working on a specific app.

```shell
# Build everything
dotnet build src/TransmissionManager.slnx

# Build API solution only (includes libraries and tests)
dotnet build src/TransmissionManager.Api.slnx

# Run all tests
dotnet test src/TransmissionManager.slnx

# Run a specific test project
dotnet test src/Tests/TransmissionManager.Database.Tests

# Run a single test by class name
dotnet test src/TransmissionManager.slnx --filter "ClassName=AddTorrentTests"

# Run a single test by fully qualified name
dotnet test src/TransmissionManager.slnx --filter "FullyQualifiedName~AddTorrentTests.AddTorrent_Returns201"
```

No separate lint command — the projects use `AnalysisLevel: latest-all` which enforces Roslyn analyzers at build time.

## Architecture

Two deployable apps backed by shared libraries:

- **TransmissionManager.Api** — ASP.NET Core Minimal API that manages torrents in a Transmission daemon. Schedules automatic downloads via cron expressions.
- **TransmissionManager.Web** — Blazor WebAssembly SPA served by Nginx. Talks to the API via a typed HTTP client.

Shared libraries:

- **TransmissionManager.Database** — EF Core + SQLite data access. Single `AppDbContext`, single `Torrent` entity, CRUD via `TorrentService`. Database is created with `EnsureCreatedAsync()` (no migrations).
- **TransmissionManager.Transmission** — Typed HTTP client for the Transmission RPC protocol. Handles `X-Transmission-Session-Id` header management and uses `HttpStandardResilienceHandler` for retries/timeouts.
- **TransmissionManager.TorrentWebPages** — HTTP client that scrapes web pages for magnet links using configurable regex patterns.
- **TransmissionManager.Api.Common** — Shared DTOs, custom validation attributes (`[Cron]`, `[MagnetRegex]`), JSON serialization contexts, and endpoint constants. Referenced by both API and Web projects.

## Key Conventions

### Endpoint structure (Vertical Slice / Action pattern)

Each API endpoint lives in its own folder under `Actions/{Feature}/{ActionName}/` and contains:

- `{Action}Endpoint.cs` — route definition via `MapXxx()`, delegates to the handler
- `{Action}Handler.cs` — business logic, receives dependencies via primary constructor
- `{Action}Result.cs` or `{Action}Outcome.cs` — result enum/tuple signaling success or failure type
- Request/response DTOs and validation as needed

Endpoints return `Results<T1, T2, ...>` discriminated unions for type-safe HTTP responses. Error responses use Problem Details (RFC 7807).

### Keyset pagination (GetPage endpoint)

The main communication surface between the Web and API projects is `GET /api/v1/torrents`, which uses **keyset (cursor) pagination** — not offset-based skip/take.

The cursor is a pair of query parameters:

- **`anchorId`** (`long?`) — the `Id` of the last item on the current page.
- **`anchorValue`** (`string?`) — the value of the current sort field for that item. `null` when ordering by `Id` alone; a formatted `DateTimeOffset` string for `RefreshDate`; or the raw string for `Name`/`WebPage`/`DownloadDir`.

All non-Id orderings use `Id` as a tiebreaker to guarantee deterministic ordering. Backward pagination reverses the sort, fetches, then re-sorts to the original order.

The response includes pre-computed `NextPageAddress` and `PreviousPageAddress` URL strings so clients don't need to construct cursors themselves:

```csharp
record GetTorrentPageResponse(
    IReadOnlyList<TorrentDto> Torrents,
    string? NextPageAddress,
    string? PreviousPageAddress);
```

Additional query parameters: `orderBy` (10 enum values covering 5 fields × asc/desc), `take` (1–1000, default 20), `direction` (Forward/Backward), `propertyStartsWith` (prefix filter across multiple string columns), `cronExists` (nullable bool filter).

The query construction logic lives in `QueryableTorrentExtensions.WhereOrderByTake` in the Database project.

### DI registration pattern

Each library exposes an `Add{Feature}Services` extension method on `IServiceCollection` (in an `Extensions/` folder). These are composed in `Program.cs`:

```csharp
builder.Services.AddDatabaseServices();
builder.Services.AddTorrentWebPagesServices(builder.Configuration);
builder.Services.AddTransmissionServices(builder.Configuration);
```

### JSON serialization

All JSON serialization uses **source-generated `JsonSerializerContext`** classes for trimming/AOT compatibility. When adding new DTOs or types that need serialization, register them in the appropriate context:

- `DtoJsonSerializerContext` (in Api.Common) — shared DTOs
- `ApiJsonSerializerContext` (in Api) — API-internal types
- `TransmissionJsonSerializerContext` (in Transmission) — Transmission RPC types

### Code organization

Prefer extracting stateful or self-contained logic into dedicated classes rather than embedding it inline in components or endpoints. This is the pattern across the codebase (e.g., handlers separated from endpoints, wrapper services around HTTP clients, `TorrentSchedulerService` wrapping Coravel scheduling).

### C# style

- Primary constructors for DI injection (no manual field declarations)
- File-scoped namespaces
- Records for DTOs
- `internal sealed` for non-public implementation classes
- `ConfigureAwait(false)` on async calls in library code

### Testing

- **NUnit 4** with `[Parallelizable(ParallelScope.Self)]`
- Shared test utilities in `TransmissionManager.BaseTests` — includes `FakeHttpMessageHandler` for mocking HTTP calls and `FakeOptionsMonitor<T>` for options
- Integration tests use `WebApplicationFactory<Program>` with fake HTTP handlers substituted for real external services
- CA1707 is suppressed in test projects to allow underscores in test method names

### Docker

Both apps have multi-stage Dockerfiles targeting `linux/amd64` and `linux/arm64`. The API uses `runtime-deps:chiseled-extra` (minimal, non-root). The Web app runs on `nginx:alpine`. Published with `PublishTrimmed=true`.
