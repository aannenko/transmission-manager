# Copilot Instructions for Transmission Manager

> **Keep this file current.** When you discover or establish a new convention, invariant, gotcha, or non-obvious design choice during a task — or notice that existing content here is stale or contradicted by the codebase — proactively suggest an update to this file (and apply it if approved). Conversely, do **not** add content that is verifiable in seconds via `grep`/`view` or already obvious from the framework conventions (e.g., "use `dotnet build`"). The goal is signal-dense agent context, not exhaustive documentation.
>
> **Keep additions high-signal, low-noise.** State the essence of a rule tersely; capture the *what* and the *why-it's-non-obvious*, not exhaustive detail. Prefer one dense sentence (plus a short example where it disambiguates) over a paragraph. If a rule needs lengthy rationale to justify it, that rationale belongs in code comments or the relevant spec doc, not here.

## Build, Test, and Lint

.NET 10 solution; central package management via `Directory.Packages.props`. Three `.slnx` files: `TransmissionManager.slnx` (full repo), `TransmissionManager.Api.slnx`, `TransmissionManager.Web.slnx` (scoped). No separate lint command — `AnalysisLevel: latest-all` runs Roslyn analyzers at build time.

Non-obvious `dotnet test` filter shapes:

```shell
dotnet test src/TransmissionManager.slnx --filter "ClassName=AddTorrentTests"
dotnet test src/TransmissionManager.slnx --filter "FullyQualifiedName~AddTorrentTests.AddTorrent_Returns201"
```

## Architecture

Two deployable apps:

- **TransmissionManager.Api** — ASP.NET Core Minimal API. Schedules cron-driven torrent refreshes via Coravel.
- **TransmissionManager.Web** — Blazor WebAssembly SPA served by Nginx.

Shared libraries:

- **TransmissionManager.Database** — EF Core + SQLite. Single `AppDbContext`, single `Torrent` entity, CRUD via `TorrentService`. Database created with `EnsureCreatedAsync()` — no migrations.
- **TransmissionManager.Transmission** — Typed HTTP client for Transmission RPC. Manages `X-Transmission-Session-Id` refresh; uses `HttpStandardResilienceHandler`.
- **TransmissionManager.TorrentWebPages** — HTTP client that scrapes magnet links via configurable regex.
- **TransmissionManager.Api.Common** — Shared DTOs, validation attributes (`[Cron]`, `[MagnetRegex]`), `JsonSerializerContext` instances, endpoint constants. Referenced by both Api and Web.

## Key Conventions

### Endpoint structure (Vertical Slice / Action pattern)

Endpoints live under `Actions/{Feature}/{ActionName}/` and combine an `{Action}Endpoint.cs`, an optional `{Action}Handler.cs`, an `{Action}Result.cs`/`{Action}Outcome.cs` enum or tuple, and DTOs. Endpoints return `Results<T1, T2, ...>` discriminated unions; errors use Problem Details (RFC 7807). `Actions/Torrents/Add/` is a representative folder.

**When to extract a Handler.** Extract when the endpoint coordinates multiple services *or* models a non-trivial Outcome union (Success / NotFound / Conflict / external-system failure / etc.). Simple pass-through endpoints keep logic inline with a private static `BuildResponse`/`ToXxxResponse` helper — `GetTorrentPageEndpoint` is the deliberate inline example.

### Keyset pagination (GetPage endpoint)

`GET /api/v1/torrents` uses **keyset (cursor) pagination**. The cursor is `anchorId` (`long?`) + `anchorValue` (`string?`, formatted per sort field; `null` when ordering by `Id` alone).

Invariants:

- All non-`Id` orderings use `Id` as a deterministic tiebreaker.
- Backward pagination **reverses** the sort, fetches `take+1` as a probe, slices from the end, then re-sorts to the original order.
- Response (`GetTorrentPageResponse`) includes pre-computed `NextPageAddress` / `PreviousPageAddress` URLs as the easy path for clients. Both are `null` at boundaries; the opposite-direction URL is emitted **only** when `parameters.AnchorId != null`.
- **Empty-page fallback**: when the DB returns an empty page and `parameters.AnchorId != null`, the opposite-direction URL is computed by `ToEmptyPageFallback` — it flips `Direction` and shifts `AnchorId` by ±1 per `bumpIdUp = isForward XOR isDescending`. The DB layer uses strict `<` / `>` comparisons, so the ±1 sentinel turns the strict bound into an inclusive one, and clicking the fallback URL returns a page that **includes** the original request's boundary item. `AnchorValue` and all filters (`PropertyStartsWith`, `CronExists`) are preserved. The sentinel saturates at `long.MaxValue` / `long.MinValue` — deliberate, because SQLite `AUTOINCREMENT` Ids start at 1 and grow monotonically, so no real row ever sits at the cap; at the cap, strict comparison degrades to pre-bump behavior (boundary excluded) only for an impossible Id. Do **not** "fix" the cap.
- `TransmissionManager.Api.Common` exposes `GetTorrentPageParameters.ToPathAndQueryString` (the Web client uses it to format the request URL). Server-side cursor-construction helpers (`ToNextPageParameters` / `ToPreviousPageParameters` / `Parse` / the empty-page fallback) live in `TransmissionManager.Api/Actions/Torrents/GetPage/` because they only shape the server's response and the Web client never reconstructs cursors itself — it follows the `NextPageAddress` / `PreviousPageAddress` strings the server already emits.

### DI registration

Each library exposes `Add{Feature}Services(IServiceCollection)` under `Extensions/`; `Program.cs` composes them. New library → new `Add{Feature}Services`.

### JSON serialization

All JSON serialization goes through **source-generated `JsonSerializerContext`** classes for trimming/AOT compatibility. New serialized types must be registered in the matching context: `DtoJsonSerializerContext` (Api.Common, shared DTOs), `ApiJsonSerializerContext` (Api, internal), `TransmissionJsonSerializerContext` (Transmission RPC).

### Code organization

Prefer extracting stateful or self-contained logic into dedicated classes (handlers split from endpoints, wrapper services around HTTP clients, `TorrentSchedulerService` wrapping Coravel). Avoid inlining anything beyond trivial in endpoints or components.

### Concurrency (OCC)

`Torrent.Version` (`long`) is the optimistic-concurrency token. `TorrentService.UpdateOneAsync` and `DeleteOneAsync` take a **required** `version` and return `TorrentMutationOutcome(TorrentMutationResult Result, long? CurrentVersion)`, where `Result` is `Success` / `NotFound` / `Conflict`; on `Conflict`, `CurrentVersion` is the current row version so the caller can retry. `[ConcurrencyCheck]` on `Version` is defence-in-depth for any future code that mutates via the EF change tracker.

### Compiled EF Core model

`AppDbContext` is wired with `UseModel(AppDbContextModel.Instance)` in `DatabaseServiceCollectionExtensions.cs` against a `dotnet ef dbcontext optimize` output checked in under `src/TransmissionManager.Database/DbContextOptimized/`. That single explicit call is the canonical wiring — it also breaks compilation if the generated file is deleted. Every other consumer (including all tests) relies on auto-discovery via the `[assembly: DbContextModel(typeof(AppDbContext), typeof(AppDbContextModel))]` attribute in `AppDbContextAssemblyAttributes.cs`. `CompiledModelTests` guards auto-discovery from silent regression.

Regenerate via `src/scripts/Optimize-DbContext.ps1`. The script accepts `-NoBuild` (CI uses this to reuse the workflow's prior `dotnet build`); `--no-build` is always forwarded to `dotnet ef dbcontext optimize`. CI's "Verify compiled EF Core model is up to date" step fails if the regenerated model differs from the checked-in copy. The `dotnet-ef` version pinned in `src/.config/dotnet-tools.json` and the `Microsoft.EntityFrameworkCore.Design` version pinned in `src/Directory.Packages.props` must be bumped together.

**Gotcha — do not "fix" the missing `Relational:Collation` annotations.** The generated `TorrentEntityType.cs` adds the five `NOCASE`-collated string properties (`HashString`, `Name`, `WebPageUri`, `DownloadDir`, `Cron`) without any collation annotation, and `IProperty.GetCollation()` throws on the read-optimized model. This is by design: the read-optimized (compiled) model carries only what the query pipeline needs; `OnModelCreating` still runs at startup and re-applies `UseCollation("NOCASE")`, so `EnsureCreatedAsync` produces `TEXT COLLATE NOCASE` columns and the unique indexes on `HashString`/`WebPageUri` remain case-insensitive. Verified end-to-end. If a reviewer flags "compiled model drops NOCASE collations", point them here.

### Independence from Transmission

TransmissionManager and the Transmission daemon are **independent systems**. The local catalog is not a mirror — a torrent may exist on one side and not the other by design. When a request mutates one side and the other side fails or races (e.g., local OCC conflict after a successful Transmission removal, or vice versa), surface the partial outcome (`409 Conflict`, `424 Failed Dependency`, etc.) and let the user retry. Do **not** introduce non-OCC fallbacks, compensating writes, or "force-finish" paths to keep the two sides in lockstep.

### C# style

Primary constructors for DI; file-scoped namespaces; records for DTOs; `internal sealed` for non-public implementations; `ConfigureAwait(false)` in library async code.

**Expression-bodied members:** use `=>` only when the body naturally fits one line (e.g. `ToDateTimeAnchorString`); otherwise use a block body.

**Member ordering:** public before private; within that, group by purpose. The public-before-private rule wins ties — a single-caller private helper still goes at the end of the type or gets inlined/nested into the caller.

**XML docs by content, not visibility:** skip boilerplate `<summary>` that restates a signature; add `<remarks>` / `<exception>` to any member (private included) only when it carries non-obvious info, e.g., an invariant, a concurrency/ordering rationale, a throw condition, or a maintainer warning.

### Naming conventions

**Extension classes always end in `Extensions`.** Pattern: `<Receiver>Extensions` for receiver-only naming (`RegexExtensions`, `ServiceCollectionExtensions`, `EndpointRouteBuilderExtensions`), or `<Receiver><Entity>Extensions` when the extension targets a specific entity or descriptor (`QueryableTorrentExtensions`, `ModelBuilderTorrentExtensions`, `DatabaseServiceCollectionExtensions`). Even single-method static helper classes follow this rule — if it's a `static class` with extension methods, it ends in `Extensions`.

Static classes that are *not* extension classes (e.g. constants holders like `PageAddresses`, EF Core `IEntityTypeConfiguration<T>` implementations) do not take the suffix.

### Testing

**NUnit 4**, parallelism `[Parallelizable(ParallelScope.Self)]`. Shared utilities in `TransmissionManager.BaseTests` (`FakeHttpMessageHandler`, `FakeOptionsMonitor<T>`). Integration tests use `WebApplicationFactory<Program>` with fake HTTP handlers; `TestWebApplicationFactory` composes on top of production DI via `ConfigureDbContext<AppDbContext>` (overriding only the SQLite connection). CA1707 is suppressed in test projects.

**Test method names are three-part: `WhatMethod_OnWhatCondition_DoesWhat`** (e.g. `GetCountAsync_WhenCalledWithFilter_ReturnsMatchingCount`, `AddTorrentAsync_WhenWebPageUriExists_ReturnsConflictResponse`). The condition segment is mandatory even when terse; keep it in the middle.

**Mirrored API ↔ DB enums.** When an API enum has a DB counterpart (e.g. `GetTorrentPageOrder` ↔ `TorrentOrder`, `GetTorrentPageDirection` ↔ `PaginationDirection`), the API gets the specific name (`Get{Action}{Concept}`) and the DB gets a reusable generic name. Cross-project value/name parity is asserted by mapping tests in `TransmissionManager.Api.Tests` (`GetTorrentPageOrderMappingTests` / `GetTorrentPageDirectionMappingTests`). The API↔DB cast (`(DbEnum)apiEnum`) is then a one-liner.

### Docker

Multi-stage Dockerfiles target `linux/amd64` and `linux/arm64`. API uses `runtime-deps:chiseled-extra` (minimal, non-root); Web uses `nginx:alpine`. Published with `PublishTrimmed=true`.
