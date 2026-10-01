# Copilot Instructions for Transmission Manager

> **Keep this file current.** When you discover or establish a new convention, invariant, gotcha, or non-obvious design choice during a task — or notice that existing content here is stale or contradicted by the codebase — proactively suggest an update to this file (and apply it if approved). Conversely, do **not** add content that is verifiable in seconds via `grep`/`view` or already obvious from the framework conventions (e.g., "use `dotnet build`"). The goal is signal-dense agent context, not exhaustive documentation.
>
> **Keep additions high-signal, low-noise.** State the essence of a rule tersely; capture the *what* and the *why-it's-non-obvious*, not exhaustive detail. Prefer one dense sentence (plus a short example where it disambiguates) over a paragraph. If a rule needs lengthy rationale to justify it, that rationale belongs in the relevant spec doc or the commit message, not here. Name only what exists: check each path, type and member you write here, and after a rename, move or deletion grep the repository, this file included, for the old name.
>
> **Where content belongs.** How a decision was reached and what was rejected goes in the change's own notes. This file gets only what would bite an agent *before* they had any reason to open the file — the rule plus a pointer, never the argument. The test is "would an agent go looking, unprompted?", not "is it discoverable". Restating one rationale in two places guarantees the two drift apart, and the stale copy is usually the one that gets read.

## Build, Test, and Lint

.NET 10 solution; central package management via `Directory.Packages.props`. Solution files: `TransmissionManager.slnx` (full repo), `TransmissionManager.Api.slnx`, `TransmissionManager.Web.slnx` (scoped). No separate lint command — `AnalysisLevel: latest-all` runs the Roslyn analyzers at build time; `EnforceCodeStyleInBuild` also includes `.editorconfig` code-style rules configured as warnings or errors, while `suggestion` rules remain editor-only.

Non-obvious `dotnet test` filter shapes — NUnit's adapter has no `ClassName` property, so match on `FullyQualifiedName`:

```shell
dotnet test src/TransmissionManager.slnx --filter "FullyQualifiedName~AddTorrentTests"
dotnet test src/TransmissionManager.slnx --filter "FullyQualifiedName~AddTorrentAsync_WhenSourceUriIsNew_AddsTorrentToTransmissionAndDb"
```

CI in the form of GitHub Actions lives in `.github/workflows/`. **A push is not finished until its runs are** — watch them (`gh run watch <id> --exit-status`) and report what they said.

## Architecture

Deployable apps:

- **TransmissionManager.Api** — ASP.NET Core Minimal API. Schedules cron-driven torrent refreshes via Coravel.
- **TransmissionManager.Web** — Blazor WebAssembly SPA served by Nginx.

Shared libraries:

- **TransmissionManager.Database** — EF Core + SQLite. Single `AppDbContext`, single `Torrent` entity, CRUD via `TorrentService`; filtered/unfiltered total counts via `TorrentCountCache` (any `TorrentService` method that changes rows or a filterable field must call `Invalidate` on its success path; see `TorrentService.<remarks>`). A torrent's magnet locator is one `SourceUri` column plus a `SourceKind` discriminator — the URI carries both *what to fetch* and *what to extract* (a `JsonPointer` source puts its RFC 6901 pointer in the fragment), so the BCL performs the split and uniqueness falls out of the single column.
- **TransmissionManager.Transmission** — Typed HTTP client for Transmission RPC. Manages `X-Transmission-Session-Id` refresh; uses `AddStandardResilienceHandler`.
- **TransmissionManager.TorrentSources** — Magnet-link sources, one vertical slice per kind: `WebPage/` scrapes a page with a configurable regex, `JsonPointer/` resolves an RFC 6901 pointer carried in the source URI's fragment against a streamed JSON document under a fixed memory bound (see `TorrentJsonPointerClientOptions.MaxJsonTokenBytes`). `Dto/` is shared by both slices; `Extensions/` holds only the DI wiring that spans them. Both report expected failures as `MagnetSearchOutcome`/`MagnetSearchResult` instead of throwing. Only `RetrievalFailed` is a dependency failure (424); the rest are caller errors (400 on add, 422 on refresh) — see `MagnetSearchResultExtensions.IsUnprocessableSource` for why `NotFound` sits on that side. Anti-bot challenges are deliberately **not** detected: recognising one vendor's would imply recognising every vendor's.
- **TransmissionManager.Api.Common** — Shared DTOs, validation attributes (`[Cron]`, `[MagnetRegex]`), `JsonSerializerContext` instances, endpoint constants. Referenced by both Api and Web.

## Key Conventions

### Choosing a pattern

**Where the codebase does one thing two ways, pick the winner and extend it — never add a third.** A new requirement is a reason to finish an existing pattern, not to start one: when half a client's methods return `SomeOutcome` and the rest throw, the way to carry a new error body is to give `SomeOutcome` somewhere to put it, not to add a `SomeException` beside both. Name the concept and find how it is already spelled here before designing.

### Endpoint structure (Vertical Slice / Action pattern)

Endpoints live under `Actions/{Feature}/{ActionName}/` and combine an `{Action}Endpoint.cs`, an optional `{Action}Handler.cs`, an `{Action}Result.cs`/`{Action}Outcome.cs` enum or tuple, and DTOs. Endpoints return `Results<T1, T2, ...>` discriminated unions; errors use Problem Details (RFC 7807). `Actions/Torrents/AddOne/` is a representative folder.

**When to extract a Handler.** Extract when the endpoint coordinates multiple services *or* models a non-trivial Outcome union (Success / NotFound / Conflict / external-system failure / etc.). Simple pass-through endpoints keep logic inline with a private static `ToXxxResponse` helper — `GetTorrentPageEndpoint` is the deliberate inline example.

**There is no OpenAPI/Swagger yet** — one is planned, and endpoint and schema documentation belongs there when it lands. Until then the feature's `.http` file (`Actions/Torrents/Torrents.http`) carries the request/response contract and must be updated in the same commit as a DTO change. `src/TransmissionManager.Api/README.md` is a quick-start for someone setting the thing up, **not** a reference — do not push everything missing into it. Verify any address a doc example fetches: a plausible-looking JSON Pointer index was wrong against the live API (measured).

### Error messages

Every failure answers with an `errors` object keyed by what is at fault rather than a prose `detail` — `EndpointProblems` builds it. A message does not repeat back the id or the body the caller just sent, though it may quote a setting to show which one it means. **Text a torrent source served must go through `RemoteTextUtils.Summarize` before it enters any message** — see that type for why. Options validators are exempt: they answer an operator at startup, not a caller.

### Keyset pagination (GetPage endpoint)

`GET /api/v1/torrents` uses **keyset (cursor) pagination**. The cursor is `anchorId` (`long?`) + `anchorValue` (`string?`, formatted per sort field; `null` when ordering by `Id` alone).

Invariants:

- All non-`Id` orderings use `Id` as a deterministic tiebreaker.
- Backward pagination **reverses** the sort, fetches `take+1` as a probe, slices from the end, then re-sorts to the original order.
- Response (`GetTorrentPageResponse`) includes pre-computed `NextPageAddress` / `PreviousPageAddress` URLs as the easy path for clients. Both are `null` at boundaries; the opposite-direction URL is emitted **only** when `parameters.AnchorId != null`.
- **Empty-page fallback**: an empty page still gets the opposite-direction address when the request had an `AnchorId`, and following it returns a page that **includes** the request's boundary item, with `AnchorValue` and every filter kept. `ToEmptyPageFallback` explains its ±1 sentinel and why the sentinel's saturation at `long.MaxValue` / `long.MinValue` must not be "fixed".
- `TransmissionManager.Api.Common` exposes `GetTorrentPageParametersExtensions.ToPathAndQueryString` (the Web client uses it to format the request URL). Server-side cursor-construction helpers (`ToNextPageParameters` / `ToPreviousPageParameters` / `Parse` / the empty-page fallback) live in `TransmissionManager.Api/Actions/Torrents/GetPage/` because only the server builds cursors; the Web client never reconstructs them.

### DI registration

Each library exposes `Add{Feature}Services(IServiceCollection)` under `Extensions/`; `Program.cs` composes them. New library → new `Add{Feature}Services`.

**Each torrent source owns its options outright.** A source's options class has no base type and binds its own `TorrentSources:{SourceKind}` subsection, so a setting two sources happen to share (`ResponseReadTimeout`, `RegexMatchTimeout`) is declared and configured once per source and tunable per source. The children are taken with `GetRequiredSection`, so a missing slice section is reported as such rather than as a handful of property failures. The cost is duplicated declarations; keep the *rationale* out of them — a fact about shared wiring belongs where that wiring is (the additive-timeout explanation lives on `ConfigureResilience`), and a slice's own doc must not describe the other slice.

**Torrent source options are validated by hand-written validators, not by data annotations.** Each options class has exactly one `IValidateOptions<T>` beside it, named `Validate{OptionsClass}`, and no validation attributes at all; that validator owns the bounds and the reasons. Its checks are ordered — its `<remarks>` say why — and the options system invokes every registered validator even after an earlier failure, then merges their failures, so a dependent check cannot wait for another validator or attribute to pass. A new property is validated only if you remember, so each validator has a test class with one case per setting.

**Gotcha — a typed HTTP client's registered name must match what the tests configure.** `TestWebApplicationFactory` installs its fake handlers by client name, and a name nothing registered is **silently ignored**, so a mismatch sends integration tests' requests out for real instead of failing. `AddHttpClient<IFoo, Foo>()` is named **`"IFoo"`**, not `"Foo"`: pin the name with `AddHttpClient<IFoo, Foo>("Foo")`, which registers **only** `IFoo`, so every consumer must inject the interface.

### Gotchas

Hard-won, each verified rather than assumed. Do not "simplify" any of these away without reproducing the failure first.

**Never send an outbound `User-Agent` naming this application.** At least one major tracker's WAF answers `HTTP 520` to any UA containing "transmission" (case-insensitive); verified across 9 variants, while sending no header, `curl/8.0` and a browser token all succeed. The app sends **no** `User-Agent` (the `HttpClient` default) and must keep doing so unless a source is measured to require one.

**`ArrayPool<T>.Shared` rents long and dirty.** `Rent(n)` rounds up to the next power-of-two bucket, so `Rent(5000)` returns 8192 — track the usable window as the size you asked for, never `buffer.Length`, or a configured limit silently drifts upward. Rented arrays are also **not** cleared, and `Return` does not clear them either, so reading past the bytes you actually filled reads whatever the previous tenant left: comparing a fixed-length prefix without first checking you read that many bytes is how one bad response poisons a bucket and faults every later one.

**Schema changes reach a deployed database only by hand, and must preserve OCC identities.** There are no migrations (`EnsureCreatedAsync` leaves a database that has tables unchanged) and, by decision, no migration or rebuild scripts, so name any schema change in your report. `TorrentService` treats `(Id, Version)` as identity: reissuing an `Id` at reset `Version = 1` lets a stale client delete or overwrite a different torrent (reproduced), while a table rebuild reseeds `sqlite_sequence.seq` from the copied rows. Stop the API container and keep it stopped before taking the final snapshot or replacing the database. EF creates the file in WAL mode, so keep the database and any `-wal` / `-shm` sidecars together until SQLite checkpoints them; never copy, read or delete the main file alone. A table rebuild must preserve every surviving row's `Id` and `Version` (increment `Version` if the change mutates torrent data) and run its replacement-table creation, copy, final swap and sequence replacement in one explicit SQLite transaction. After the final rename but before commit, replace the `Torrents` sequence row with at least `max(saved seq, max(Id))` by `DELETE` followed by `INSERT`, then verify that floor; keep the API stopped until the committed database passes integrity and query checks. For a recreated file, run schema creation isolated from seeds, clients and background work, stop it, set that counter floor, then start normally and re-add. `ALTER TABLE … ADD COLUMN` preserves the sequence.

**An enum in a JSON body needs `[JsonConverter(typeof(JsonStringEnumConverter<T>))]` on the enum itself.** `DtoJsonSerializerContext`'s `UseStringEnumConverter = true` governs only what is written *through that context*; a consumer reading the response with its own options (e.g. an integration test's bare `ReadFromJsonAsync<T>()`) still expects a number and fails on the string. The attribute makes the enum round-trip regardless of caller options — `TransmissionAddResult` and `TorrentSourceKind` carry it. Query-string enums (`GetTorrentPageOrder`, `GetTorrentPageDirection`) do not need it; they never pass through a JSON body.

### JSON serialization

All JSON serialization goes through **source-generated `JsonSerializerContext`** classes for trimming/AOT compatibility. Register a type `Api` and `Web` exchange in `DtoJsonSerializerContext`, and anything else in the context of the project that serializes it.

### Code organization

Prefer extracting stateful or self-contained logic into dedicated classes (handlers split from endpoints, wrapper services around HTTP clients, `TorrentSchedulerService` wrapping Coravel). Avoid inlining anything beyond trivial in endpoints or components.

### Concurrency (OCC)

`Torrent.Version` (`long`) is the optimistic-concurrency token. `TorrentService.UpdateOneAsync` and `DeleteOneAsync` take a **required** `version` and return `TorrentMutationOutcome`; `TorrentMutationResult` documents which results carry `CurrentVersion` and which a caller can retry. `[ConcurrencyCheck]` on `Version` is defence-in-depth for any future code that mutates via the EF change tracker.

### Compiled EF Core model

`AppDbContext` is wired with `UseModel(AppDbContextModel.Instance)` in `DatabaseServiceCollectionExtensions.cs` against a `dotnet ef dbcontext optimize` output checked in under `src/TransmissionManager.Database/DbContextOptimized/`. That single explicit call is the canonical wiring — it also breaks compilation if the generated file is deleted. Every other consumer (including all tests) relies on auto-discovery via the `[assembly: DbContextModel(typeof(AppDbContext), typeof(AppDbContextModel))]` attribute in `AppDbContextAssemblyAttributes.cs`. `CompiledModelTests` guards auto-discovery from silent regression.

Regenerate via `src/scripts/Optimize-DbContext.ps1`. The script accepts `-NoBuild` (CI uses this to reuse the workflow's prior `dotnet build`); `--no-build` is always forwarded to `dotnet ef dbcontext optimize`. CI's "Verify compiled EF Core model is up to date" step fails if the regenerated model differs from the checked-in copy. The `dotnet-ef` version pinned in `src/.config/dotnet-tools.json` and the `Microsoft.EntityFrameworkCore.Design` version pinned in `src/Directory.Packages.props` must be bumped together.

**Gotcha — do not "fix" the missing `Relational:Collation` annotations** in the generated `TorrentEntityType.cs`. The read-optimized model carries only what queries need, so `IProperty.GetCollation()` throws on it by design. When `EnsureCreatedAsync` creates the schema, EF uses the full design-time model built by `OnModelCreating`, so the created columns and unique indexes on `HashString` / `SourceUri` retain `NOCASE`; existing schemas are not repaired on startup. `TorrentServiceCommandTests` pins schema creation. If a reviewer flags "compiled model drops NOCASE collations", point them here.

**Decision — keep `SourceUri` uniqueness case-insensitive.** `NOCASE` applies to the whole URI, including a JSON Pointer fragment whose member names are case-sensitive; leave the schema and index unchanged unless the owner explicitly reopens this decision.

### Independence from Transmission

TransmissionManager and the Transmission daemon are **independent systems**. The local catalog is not a mirror — a torrent may exist on one side and not the other by design. When a request mutates one side and the other side fails or races (e.g., local OCC conflict after a successful Transmission removal, or vice versa), surface the partial outcome (`409 Conflict`, `424 Failed Dependency`, etc.) and let the user retry. Do **not** introduce non-OCC fallbacks, compensating writes, or "force-finish" paths to keep the two sides in lockstep.

### Web app

**Supported browsers are Chromium, the five latest versions.** Firefox and Safari behaviour is not a release criterion, and a feature may rely on Chromium-specific rendering.

Text fields are `<InputText type="search" role="textbox">`, which makes Chromium draw the clear button itself — the app ships no markup, CSS or icon for it. The `role` is there because `type="search"` otherwise announces every field as a search box; the type is presentational, the role is the truth. Two consequences were accepted deliberately: the button appears only while the field is focused, and Firefox draws none at all. **Carry both attributes on every new text field** — nothing in the build or the tests catches their absence.

### C# style

Primary constructors for DI; file-scoped namespaces; records for DTOs; `internal sealed` for non-public implementations; `ConfigureAwait(false)` in library async code.

**Expression-bodied members:** use `=>` only when the body naturally fits one line (e.g. `ToDateTimeAnchorString`); otherwise use a block body.

**Member ordering:** public before private; within that, group by purpose. The public-before-private rule wins ties — a single-caller private helper still goes at the end of the type or gets inlined/nested into the caller.

**Comments: none by default** — in production code, tests, helpers, fakes and test data alike. A comment may hold only what this code does not give up in seconds, stated once, next to the code it is about. So never who calls this code, how a caller works or what it does with the result, or what this code is for or lets a caller do (`so a test can…`) — if adding or removing a caller would falsify a sentence, delete it; a requirement on callers or a guarantee to them is contract and stays even where a test also pins it, and so is a warning about what breaks if this code changes. Never what a test asserts, a restatement of the name, signature or next line, or the history of a change (the commit message holds that). Any other claim about what the code does that a test could check should be that test instead: both say the same thing, but only the test fails when it stops being true. The reason the code is shaped that way stays. Never state a mechanism you have not checked against the code.

**Mandated comments**, the only ones written by default:
- *Full XML docs* on every `public` type or member used across production-project boundaries, directly or through framework discovery; same-project framework use alone does not qualify. An in-scope member also puts its declaring type in scope, while tests and `BaseTests` never make production code in scope. A primary constructor (including a positional record) documents its parameters on the type, and an interface implementation or override may use `<inheritdoc/>`. Otherwise the full form is `<summary>`, a `<param>` per parameter, a `<typeparam>` per type parameter, `<returns>` unless void or a constructor, and an `<exception>` per exception it throws itself; only `ArgumentNullException.ThrowIfNull` is exempt. A required tag with nothing to add restates the signature in one line, since a missing one renders as a blank. Worked form: `TorrentWebPageClient.FindMagnetUriAsync`.
- *A suppression's reason*, on its `#pragma warning disable` line.
- *A torrent source options class's `<remarks>`* naming the `IValidateOptions<T>` that owns its bounds.
- *The last undocumented member* of one enum (`MagnetSearchResult.Found`), one overload group or one block's tags whose others are documented — never extended to sibling types.

Tool directives (`// language=regex`) and upstream's comments in vendored code (`ValueStringBuilder`) are exempt.

**State a shared convention once, on the type.** `UpdateTorrentByIdRequest` puts its null-is-ignored / empty-clears rule in the type's `<remarks>` and names there the one property that differs, instead of repeating the rule on every property.

**Never name a type the project cannot reference.** The libraries (`Api.Common`, `Database`, `TorrentSources`, `Transmission`) declare no `ProjectReference`s — only `Api` and `Web` do — so a comment in one of them naming a type from another project (test classes included) points at something the reader cannot navigate to, and nothing catches it: `GenerateDocumentationFile` is off repo-wide, so even `<see cref>` goes unvalidated (measured). Put the cross-project statement where the reference direction actually runs — in the consuming project, in a test, or in this file. The mirrored `TorrentSourceKind` pair is the worked example: the `Database` copy documents its own storage contract, the `Api.Common` copy says nothing about the database, and their parity is asserted by a test in `Api.Tests`.

**Before reporting a `.cs` or `.razor` change done**, list the comment lines you added or changed — `git diff -U0 <start> -- '*.cs' '*.razor' | Select-String '^\+\+\+ |^\+(?!\+\+ ).*?(?<!:)//|^\+\s*/\*|^\+.*(@\*|<!--)'` against the commit your task started from, and `git ls-files -o --exclude-standard -- '*.cs' '*.razor' | ForEach-Object { Select-String -LiteralPath $_ -Pattern '(?<!:)//|^\s*/\*|@\*|<!--' }` for new untracked files — and delete each line the first paragraph does not allow. In a mandated tag, remove prohibited caller/history prose without erasing its contract; use a one-line signature restatement only when nothing allowed remains.

### Naming conventions

**Extension classes always end in `Extensions`.** Pattern: `<Receiver>Extensions` for receiver-only naming (`RegexExtensions`, `ServiceCollectionExtensions`, `EndpointRouteBuilderExtensions`), or `<Receiver><Entity>Extensions` when the extension targets a specific entity or descriptor (`QueryableTorrentExtensions`, `ServiceProviderTorrentSourceExtensions`). Even single-method static helper classes follow this rule — if it's a `static class` with extension methods, it ends in `Extensions`.

Static classes that are *not* extension classes (e.g. constants holders like `PageAddresses`) do not take the suffix.

**Placement follows scope, not file type.** In `TransmissionManager.Database` and `TransmissionManager.Transmission` every extension class lives in `Extensions/`. The vertically sliced projects — `TransmissionManager.TorrentSources` and `TransmissionManager.Api` — instead keep `Extensions/` for wiring that spans slices only (`TorrentSourcesServiceCollectionExtensions`, `CorsServiceCollectionExtensions`, `StartupLoggerExtensions`); anything a single slice owns lives with that slice — `Actions/{Feature}/{Action}/` when one action uses it (`Actions/Torrents/GetPage/GetTorrentPageOrderExtensions.cs`), `Actions/{Feature}/` when several do (`Actions/Torrents/TorrentExtensions.cs`) — and a non-wiring helper both torrent-source slices use sits at the project root (`RegexExtensions.cs`). A slice owns its options and validator too (`JsonPointer/TorrentJsonPointerClientOptions.cs`). `Services/` is for stateful services and their DTOs — never for extension classes.

### Testing

**NUnit 4.** A fixture takes `[Parallelizable(ParallelScope.All)]` only when its tests share no mutable state: NUnit runs all of a fixture's tests on one instance, so its fields and `[OneTimeSetUp]` resources are shared. Otherwise use `ParallelScope.Self`. Shared utilities in `TransmissionManager.BaseTests` (`FakeHttpMessageHandler`, `FakeOptionsMonitor<T>`). Integration tests use `WebApplicationFactory<Program>` with fake HTTP handlers; `TestWebApplicationFactory` composes on top of production DI via `ConfigureDbContext<AppDbContext>` (overriding only the SQLite connection).

**Test method names are three-part: `WhatMethod_OnWhatCondition_DoesWhat`** (e.g. `GetCountAsync_WhenCalledWithFilter_ReturnsMatchingCount`, `AddTorrentAsync_WhenSourceUriExists_ReturnsConflictResponse`). The condition segment is mandatory even when terse; keep it in the middle.

**A `[TestCase]` name must carry the method name.** NUnit *replaces* the generated name with `TestName` rather than appending to it, so a bare description like `"empty"` loses the method entirely — and repeats across classes. Omit `TestName` whenever the generated `Method(args)` renders legibly; write one only when an argument does not (control characters, an empty string, a very long literal), and then spell out the whole thing: `TestName = "TryParseAsArrayIndex_WhenTokenIsNotAnIndex_Fails(digit then NUL)"`.

**Mirrored API ↔ DB enums.** When an API enum has a DB counterpart (e.g. `GetTorrentPageOrder` ↔ `TorrentOrder`, `GetTorrentPageDirection` ↔ `PaginationDirection`), the API gets the specific name (`Get{Action}{Concept}`) and the DB gets a reusable generic name. That split applies only when the API name is action-scoped; a concept that reads the same on both sides keeps **one name in two namespaces** (`TorrentSourceKind`), disambiguated at use sites with a `DbSourceKind` / `ApiSourceKind` alias. Cross-project value/name parity is asserted by mapping tests in `TransmissionManager.Api.Tests` (`GetTorrentPageOrderMappingTests` / `GetTorrentPageDirectionMappingTests` / `TorrentSourceKindMappingTests`). The API↔DB cast (`(DbEnum)apiEnum`) is then a one-liner. An enum that is **persisted** (`TorrentSourceKind`) additionally treats its numeric values as a storage contract — renaming a member is safe, reassigning its value silently reinterprets every stored row, so the values are pinned by their own test.

### Docker

Multi-stage Dockerfiles target `linux/amd64` and `linux/arm64`. API uses `runtime-deps:chiseled-extra` (minimal, non-root); Web uses `nginx:alpine`. Published with `PublishTrimmed=true`.
