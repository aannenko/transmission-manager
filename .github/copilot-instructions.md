# Copilot Instructions for Transmission Manager

> **Keep this file current.** When you discover or establish a new convention, invariant, gotcha, or non-obvious design choice during a task — or notice that existing content here is stale or contradicted by the codebase — proactively suggest an update to this file (and apply it if approved). Conversely, do **not** add content that is verifiable in seconds via `grep`/`view` or already obvious from the framework conventions (e.g., "use `dotnet build`"). The goal is signal-dense agent context, not exhaustive documentation.
>
> **Keep additions high-signal, low-noise.** State the essence of a rule tersely; capture the *what* and the *why-it's-non-obvious*, not exhaustive detail. Prefer one dense sentence (plus a short example where it disambiguates) over a paragraph. If a rule needs lengthy rationale to justify it, that rationale belongs in code comments or the relevant spec doc, not here.
>
> **Where content belongs.** What a value or type *means* goes on its own declaration; why a policy chose one mapping over another goes on the code implementing that policy; how a decision was reached and what was rejected goes in the change's own notes. This file gets only what would bite an agent *before* they had any reason to open the file — the rule plus a pointer, never the argument. The test is "would an agent go looking, unprompted?", not "is it discoverable". Restating one rationale in two places guarantees the two drift apart, and the stale copy is usually the one that gets read.

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

- **TransmissionManager.Database** — EF Core + SQLite. Single `AppDbContext`, single `Torrent` entity, CRUD via `TorrentService`; filtered/unfiltered total counts via `TorrentCountCache` (any `TorrentService` method that changes rows or a filterable field must call `Invalidate` on its success path; see `TorrentService.<remarks>`). A torrent's magnet locator is one `SourceUri` column plus a `SourceKind` discriminator — the URI carries both *what to fetch* and *what to extract* (a `JsonPointer` source puts its RFC 6901 pointer in the fragment), so the BCL performs the split and uniqueness falls out of the single column.
- **TransmissionManager.Transmission** — Typed HTTP client for Transmission RPC. Manages `X-Transmission-Session-Id` refresh; uses `HttpStandardResilienceHandler`.
- **TransmissionManager.TorrentSources** — Magnet-link sources, one vertical slice per kind: `WebPage/` scrapes a page with a configurable regex, `JsonPointer/` resolves an RFC 6901 pointer carried in the source URI's fragment against a streamed JSON document under a fixed memory bound (see `TorrentJsonPointerClientOptions.MaxJsonTokenBytes`). `Dto/` and `Options/` are shared by both slices; `Extensions/` holds only the DI wiring that spans them. Both report expected failures as `MagnetSearchOutcome`/`MagnetSearchResult` instead of throwing. Only `RetrievalFailed` is a dependency failure (424); the rest are caller errors (400 on add, 422 on refresh) — see `MagnetSearchResultExtensions.IsUnprocessableSource` for why `NotFound` sits on that side. Anti-bot challenges are deliberately **not** detected: recognising one vendor's would imply recognising every vendor's.
- **TransmissionManager.Api.Common** — Shared DTOs, validation attributes (`[Cron]`, `[MagnetRegex]`), `JsonSerializerContext` instances, endpoint constants. Referenced by both Api and Web.

## Key Conventions

### Endpoint structure (Vertical Slice / Action pattern)

Endpoints live under `Actions/{Feature}/{ActionName}/` and combine an `{Action}Endpoint.cs`, an optional `{Action}Handler.cs`, an `{Action}Result.cs`/`{Action}Outcome.cs` enum or tuple, and DTOs. Endpoints return `Results<T1, T2, ...>` discriminated unions; errors use Problem Details (RFC 7807). `Actions/Torrents/Add/` is a representative folder.

**When to extract a Handler.** Extract when the endpoint coordinates multiple services *or* models a non-trivial Outcome union (Success / NotFound / Conflict / external-system failure / etc.). Simple pass-through endpoints keep logic inline with a private static `BuildResponse`/`ToXxxResponse` helper — `GetTorrentPageEndpoint` is the deliberate inline example.

**There is no OpenAPI/Swagger**, so `src/TransmissionManager.Api/README.md` and the `.http` file beside each action *are* the API's contract documentation — a change to a request/response DTO has no other way to reach a user and must update them in the same commit. Verify any address a doc example fetches: a plausible-looking JSON Pointer index was wrong against the live API (measured).

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

**Per-source options inherit rather than compose.** Every torrent source's options class derives from `TorrentSourcesOptions` and binds the same `TorrentSources` section, so a setting all sources need is declared once on the base, and one only some need is declared in each that reads it — either way it stays a single configuration key. Never add an options class for a subset of sources. The cost is that a shared setting is validated once per source, so a bad value surfaces as several failures rather than one.

**Gotcha — a typed HTTP client's registered name must match what the tests configure.** `AddHttpClient<TConcrete>()` names the client `"TConcrete"`, but `AddHttpClient<IFoo, Foo>()` names it **`"IFoo"`**. `TestWebApplicationFactory` installs its fake handlers via `services.PostConfigure(nameof(TConcrete), …)` — a *string* key — and `PostConfigure` against an unregistered name is **silently ignored**, so a mismatch makes integration tests issue real outbound requests instead of failing. Registering behind an interface is fine as long as the name is pinned explicitly: `AddHttpClient<IFoo, Foo>("Foo")` keeps the name `"Foo"` (verified). Note that overload registers **only** `IFoo` — the concrete type stops being resolvable, so every consumer must inject the interface.

### Gotchas

Hard-won, each verified rather than assumed. Do not "simplify" any of these away without reproducing the failure first.

**Never send an outbound `User-Agent` naming this application.** At least one major tracker's WAF answers `HTTP 520` to any UA containing "transmission" (case-insensitive); verified across 9 variants, while sending no header, `curl/8.0` and a browser token all succeed. The app sends **no** `User-Agent` (the `HttpClient` default) and must keep doing so unless a source is measured to require one.

**`ArrayPool<T>.Shared` rents long and dirty.** `Rent(n)` rounds up to the next power-of-two bucket, so `Rent(5000)` returns 8192 — track the usable window as the size you asked for, never `buffer.Length`, or a configured limit silently drifts upward. Rented arrays are also **not** cleared, and `Return` does not clear them either, so reading past the bytes you actually filled reads whatever the previous tenant left: comparing a fixed-length prefix without first checking you read that many bytes is how one bad response poisons a bucket and faults every later one.

**Preserve `sqlite_sequence.seq` across a rebuild.** `TorrentService`'s OCC disambiguation relies on `AUTOINCREMENT` never reusing a deleted row's `Id`. EF Core emits `AUTOINCREMENT`, so a database it creates maintains the counter for you — but a hand rebuild reseeds `seq` from the copied rows, handing out again every `Id` above the surviving maximum. `Version` does not cover for it: it starts at `1` for every torrent, so a stale `(Id, 1)` token matches a brand-new row and deletes or overwrites a torrent the user never saw (reproduced). Restore the counter explicitly after any rebuild.

**An enum in a JSON body needs `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` on the enum itself.** The `JsonSerializerContext`s' `UseStringEnumConverter = true` governs only what is written *through that context*; a consumer reading the response with its own options (e.g. an integration test's bare `ReadFromJsonAsync<T>()`) still expects a number and fails on the string. The attribute makes the enum round-trip regardless of caller options — `TransmissionAddResult` and `TorrentSourceKind` carry it. Query-string enums (`GetTorrentPageOrder`, `GetTorrentPageDirection`) do not need it; they never pass through a JSON body.

### JSON serialization

All JSON serialization goes through **source-generated `JsonSerializerContext`** classes for trimming/AOT compatibility. New serialized types must be registered in the matching context: `DtoJsonSerializerContext` (Api.Common, shared DTOs), `ApiJsonSerializerContext` (Api, internal), `TransmissionJsonSerializerContext` (Transmission RPC).

### Code organization

Prefer extracting stateful or self-contained logic into dedicated classes (handlers split from endpoints, wrapper services around HTTP clients, `TorrentSchedulerService` wrapping Coravel). Avoid inlining anything beyond trivial in endpoints or components.

### Concurrency (OCC)

`Torrent.Version` (`long`) is the optimistic-concurrency token. `TorrentService.UpdateOneAsync` and `DeleteOneAsync` take a **required** `version` and return `TorrentMutationOutcome(TorrentMutationResult Result, long? CurrentVersion)`, where `Result` is `Success` / `NotFound` / `Conflict`; on `Conflict`, `CurrentVersion` is the current row version so the caller can retry. `[ConcurrencyCheck]` on `Version` is defence-in-depth for any future code that mutates via the EF change tracker.

### Compiled EF Core model

`AppDbContext` is wired with `UseModel(AppDbContextModel.Instance)` in `DatabaseServiceCollectionExtensions.cs` against a `dotnet ef dbcontext optimize` output checked in under `src/TransmissionManager.Database/DbContextOptimized/`. That single explicit call is the canonical wiring — it also breaks compilation if the generated file is deleted. Every other consumer (including all tests) relies on auto-discovery via the `[assembly: DbContextModel(typeof(AppDbContext), typeof(AppDbContextModel))]` attribute in `AppDbContextAssemblyAttributes.cs`. `CompiledModelTests` guards auto-discovery from silent regression.

Regenerate via `src/scripts/Optimize-DbContext.ps1`. The script accepts `-NoBuild` (CI uses this to reuse the workflow's prior `dotnet build`); `--no-build` is always forwarded to `dotnet ef dbcontext optimize`. CI's "Verify compiled EF Core model is up to date" step fails if the regenerated model differs from the checked-in copy. The `dotnet-ef` version pinned in `src/.config/dotnet-tools.json` and the `Microsoft.EntityFrameworkCore.Design` version pinned in `src/Directory.Packages.props` must be bumped together.

**Gotcha — do not "fix" the missing `Relational:Collation` annotations.** The generated `TorrentEntityType.cs` adds the five `NOCASE`-collated string properties (`HashString`, `Name`, `SourceUri`, `DownloadDir`, `Cron`) without any collation annotation, and `IProperty.GetCollation()` throws on the read-optimized model. This is by design: the read-optimized (compiled) model carries only what the query pipeline needs; `OnModelCreating` still runs at startup and re-applies `UseCollation("NOCASE")`, so `EnsureCreatedAsync` produces `TEXT COLLATE NOCASE` columns and the unique indexes on `HashString`/`SourceUri` remain case-insensitive. Verified end-to-end. If a reviewer flags "compiled model drops NOCASE collations", point them here.

### Independence from Transmission

TransmissionManager and the Transmission daemon are **independent systems**. The local catalog is not a mirror — a torrent may exist on one side and not the other by design. When a request mutates one side and the other side fails or races (e.g., local OCC conflict after a successful Transmission removal, or vice versa), surface the partial outcome (`409 Conflict`, `424 Failed Dependency`, etc.) and let the user retry. Do **not** introduce non-OCC fallbacks, compensating writes, or "force-finish" paths to keep the two sides in lockstep.

### C# style

Primary constructors for DI; file-scoped namespaces; records for DTOs; `internal sealed` for non-public implementations; `ConfigureAwait(false)` in library async code.

**Expression-bodied members:** use `=>` only when the body naturally fits one line (e.g. `ToDateTimeAnchorString`); otherwise use a block body.

**Member ordering:** public before private; within that, group by purpose. The public-before-private rule wins ties — a single-caller private helper still goes at the end of the type or gets inlined/nested into the caller.

**XML docs by content, not visibility:** skip boilerplate `<summary>` that restates a signature; add `<remarks>` / `<exception>` to any member (private included) only when it carries non-obvious info, e.g., an invariant, a concurrency/ordering rationale, a throw condition, or a maintainer warning.

**Never name a type the project cannot reference.** The four libraries (`Api.Common`, `Database`, `TorrentSources`, `Transmission`) declare no `ProjectReference`s — only `Api` and `Web` do — so a comment in one of them naming a type from another project (test classes included) points at something the reader cannot navigate to, and nothing catches it: `GenerateDocumentationFile` is off repo-wide, so even `<see cref>` goes unvalidated (measured). Put the cross-project statement where the reference direction actually runs — in the consuming project, in a test, or in this file. The mirrored `TorrentSourceKind` pair is the worked example: the `Database` copy documents its own storage contract, the `Api.Common` copy says nothing about the database, and their parity is asserted by a test in `Api.Tests`.

### Naming conventions

**Extension classes always end in `Extensions`.** Pattern: `<Receiver>Extensions` for receiver-only naming (`RegexExtensions`, `ServiceCollectionExtensions`, `EndpointRouteBuilderExtensions`), or `<Receiver><Entity>Extensions` when the extension targets a specific entity or descriptor (`QueryableTorrentExtensions`, `ModelBuilderTorrentExtensions`, `DatabaseServiceCollectionExtensions`). Even single-method static helper classes follow this rule — if it's a `static class` with extension methods, it ends in `Extensions`.

Static classes that are *not* extension classes (e.g. constants holders like `PageAddresses`, EF Core `IEntityTypeConfiguration<T>` implementations) do not take the suffix.

**Placement follows scope, not file type.** In `TransmissionManager.Database` and `TransmissionManager.Transmission` every extension class lives in `Extensions/`. The vertically sliced projects — `TransmissionManager.TorrentSources` and `TransmissionManager.Api` — instead keep `Extensions/` for wiring that spans slices only (`TorrentSourcesServiceCollectionExtensions`, `CorsServiceCollectionExtensions`, `StartupLoggerExtensions`); anything a single slice owns lives with that slice: `WebPage/RegexExtensions.cs`, or `Actions/{Feature}/{Action}/` when one action uses it (`Actions/Torrents/GetPage/GetTorrentPageOrderExtensions.cs`), or `Actions/{Feature}/` when several do (`Actions/Torrents/TorrentExtensions.cs`). A slice owns its options and validator too (`JsonPointer/TorrentJsonPointerClientOptions.cs`), leaving `Options/` for genuinely shared settings. `Services/` is for stateful services and their DTOs — never for extension classes.

### Testing

**NUnit 4**, parallelism `[Parallelizable(ParallelScope.Self)]`. Shared utilities in `TransmissionManager.BaseTests` (`FakeHttpMessageHandler`, `FakeOptionsMonitor<T>`). Integration tests use `WebApplicationFactory<Program>` with fake HTTP handlers; `TestWebApplicationFactory` composes on top of production DI via `ConfigureDbContext<AppDbContext>` (overriding only the SQLite connection). CA1707 is suppressed in test projects.

**Test method names are three-part: `WhatMethod_OnWhatCondition_DoesWhat`** (e.g. `GetCountAsync_WhenCalledWithFilter_ReturnsMatchingCount`, `AddTorrentAsync_WhenSourceUriExists_ReturnsConflictResponse`). The condition segment is mandatory even when terse; keep it in the middle.

**A `[TestCase]` name must carry the method name.** NUnit *replaces* the generated name with `TestName` rather than appending to it, so a bare description like `"empty"` loses the method entirely — and repeats across classes. Omit `TestName` whenever the generated `Method(args)` renders legibly; write one only when an argument does not (control characters, an empty string, a very long literal), and then spell out the whole thing: `TestName = "TryParseAsArrayIndex_WhenTokenIsNotAnIndex_Fails(digit then NUL)"`.

**Mirrored API ↔ DB enums.** When an API enum has a DB counterpart (e.g. `GetTorrentPageOrder` ↔ `TorrentOrder`, `GetTorrentPageDirection` ↔ `PaginationDirection`), the API gets the specific name (`Get{Action}{Concept}`) and the DB gets a reusable generic name. That split applies only when the API name is action-scoped; a concept that reads the same on both sides keeps **one name in two namespaces** (`TorrentSourceKind`), disambiguated at use sites with a `DbSourceKind` / `ApiSourceKind` alias. Cross-project value/name parity is asserted by mapping tests in `TransmissionManager.Api.Tests` (`GetTorrentPageOrderMappingTests` / `GetTorrentPageDirectionMappingTests` / `TorrentSourceKindMappingTests`). The API↔DB cast (`(DbEnum)apiEnum`) is then a one-liner. An enum that is **persisted** (`TorrentSourceKind`) additionally treats its numeric values as a storage contract — renaming a member is safe, reassigning its value silently reinterprets every stored row, so the values are pinned by their own test.

### Docker

Multi-stage Dockerfiles target `linux/amd64` and `linux/arm64`. API uses `runtime-deps:chiseled-extra` (minimal, non-root); Web uses `nginx:alpine`. Published with `PublishTrimmed=true`.
