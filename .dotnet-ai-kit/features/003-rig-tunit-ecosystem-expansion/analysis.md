# Analysis Report: Rig.TUnit Ecosystem Expansion

**Feature**: 003-rig-tunit-ecosystem-expansion | **Mode**: Generic (single-repo library ecosystem)
**Date**: 2026-04-17 | **Findings**: 17 | **Status**: ALL RESOLVED (2026-04-17)

## Summary

| Severity | Count | Resolved |
|---|---|---|
| CRITICAL | 0 | — |
| HIGH | 3 | ✅ 3/3 |
| MEDIUM | 8 | ✅ 8/8 |
| LOW | 6 | ✅ 6/6 |

## Resolution Log (2026-04-17 — fix pass)

| # | Severity | Resolution |
|---|---|---|
| F1 | HIGH | Fixture tasks T061, T071, T101, T131, T211, T221, T231, T251, T261 updated to create paired `*FixtureOptions` / `*BuilderOptions` classes with `SectionName`, `[Required]` props, and `ValidateOnStart()`. |
| F2 | HIGH | `IsolationKey` pinned to `src/Rig.TUnit.Core/IsolationKey.cs` in both T042 and data-model.md. Rationale recorded. |
| F3 | HIGH | Added per-phase README tasks T159 (Phase A), T289 (Phase B), T369 (Phase C), T469 (Phase D), T719 (Phase E). Each phase's merge gate now requires READMEs present. |
| F4 | MEDIUM | Redis builders renamed: `RedisCacheRigBuilder` (cache role) + `UseRedisCache(...)`; `RedisKvRigBuilder` (KV role) + `UseRedisKv(...)`. Bare `UseRedis` forbidden — enforced by arch test. T102, T111, quickstart.md, data-model.md updated. |
| F5 | MEDIUM | Per-area extensions file convention documented in data-model.md §"RigBuilder fluent-chain entry-points". Each provider/area ships its own `*RigBuilderExtensions.cs` decorating `RigBuilder`; Core stays minimal. |
| F6 | MEDIUM | T030 rewritten with explicit implementation note: custom reflection across `Rig.TUnit.*.Tests.*.dll` assemblies, fallback naming heuristic, whitelist file for exceptions. |
| F7 | MEDIUM | T053 and data-model.md `SqlRigBuilder<TSelf>` explicitly enumerate `ReplaceDbContext<TContext>()` + overload as promoted/inherited methods. |
| F8 | MEDIUM | Paired `Tests.Integration` tasks added for Phase E providers: T601 (Oracle), T611 (Dynamo), T621 (Cassandra), T631 (EventStore), T641 (ElasticSearch), T651 (Sqs), T661 (Nats), T671 (MinIO), T681 (FileSystem). |
| F9 | MEDIUM | US9 Acceptance Scenario 1 in spec.md annotated with "acceptance met at end of Phase D, not Phase C". T370 Phase C merge gate clarified. |
| F10 | MEDIUM | T223 and data-model.md state: `AdditionalPiiPatterns` is ECMAScript regex, case-insensitive, compiled once at startup. |
| F11 | MEDIUM | C-006 added resolving anti-pattern detector mechanism = runtime detector + Roslyn analyzer (hybrid). New tasks T227 (analyzer package) + T228 (analyzer tests). Plan.md package tree updated. |
| F12 | MEDIUM | T151 commits to `tests/Rig.TUnit.Parallelism.Tests.Contract/` as stub project created in Phase A; source package ships in Phase E. |
| F13 | LOW | T006 added — `.githooks/commit-msg` + GitHub Actions backstop enforcing `test:`/`feat:`/`refactor:`/... prefixes. |
| F14 | LOW | quickstart.md "Your first test" now shows proper `MyTestRig : CompositeFixture` derivation before consumption. |
| F15 | LOW | T007 added — capture pre-cutover benchmark baseline to `benchmarks/baseline-002.json` before any Phase A work. |
| F16 | LOW | T702 requires `Rig.TUnit.All` to be pure meta-package (zero source files). New T703 adds architecture rule `MetaPackages_HaveZeroSourceFiles` enforcing for all 3 meta-packages. |
| F17 | LOW | FR-110 in spec.md now lists exact 4 packages (Core + Mediator + Grpc + WebAPI). "common" ambiguity removed. |

**Artifacts loaded**: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md, checklists/requirements.md.

**Passes run**: Architecture Consistency, Naming Consistency, Coverage Gaps, Concurrency.
**Passes skipped**: Microservice passes 5–11 (generic mode — no event-flow.md, service-map.md, or cross-repo contracts).

---

## Findings

### [HIGH] Coverage Gaps: No explicit task creates `*FixtureOptions` classes

**Location**: spec.md FR-054, data-model.md §"Options Classes", tasks.md T061/T071/T101/T131
**Details**: FR-054 mandates that "every fixture configuration MUST use the Options pattern (`[Required]` + `ValidateOnStart()`)". Data-model.md enumerates the required Options types (`SqlServerFixtureOptions`, `SqliteFixtureOptions`, `RedisFixtureOptions`, `ServiceBusFixtureOptions`, `SeqFixtureOptions`, `JwtFixtureOptions`, `OAuthFixtureOptions`, `LoggingDetectorOptions`, etc.). However, the fixture-creation tasks (T061 for SqlServerFixture, T071 for SqliteFixture, T101 for RedisFixture, T131 for ServiceBusFixture) mention inheriting `*FixtureBase` but do not enumerate the paired Options class as a distinct deliverable. Only `LoggingDetectorOptions` (T223) is called out explicitly. Risk: implementation may ship fixtures with ad-hoc constructors, violating `.claude/rules/configuration.md` + FR-054.
**Suggested Fix**: Expand each fixture task description to include the paired `*FixtureOptions` class with `SectionName`, `[Required]` properties, and `ValidateOnStart()` wiring. Alternatively, add a single companion task per fixture package (e.g., T061a "Create SqlServerFixtureOptions").

---

### [HIGH] Architecture Consistency: `IsolationKey` location is ambiguous

**Location**: data-model.md §"IsolationKey record", tasks.md T042
**Details**: Data-model.md says `IsolationKey` "can live in `Rig.TUnit.Databases` or `Rig.TUnit.Core`"; T042 file path is `src/Rig.TUnit.Databases/IsolationKey.cs (or Rig.TUnit.Core)`. But `IsolationKey` is consumed by every area — Messaging (topic suffixes), Caching (prefixes), Storage (containers), Observability (no collision needed but traceable), Security (nonce seeds). If it lives in `Rig.TUnit.Databases`, every non-database base must reference `Rig.TUnit.Databases` — violating the "base NEVER references sibling base" dependency rule that the architecture tests enforce (T021–T024).
**Suggested Fix**: Pin `IsolationKey` in `Rig.TUnit.Core/IsolationKey.cs`. Update T042 file path accordingly. Update data-model.md to remove the "or Rig.TUnit.Core" fork.

---

### [HIGH] Coverage Gaps: No task creates per-package README files

**Location**: spec.md SC-006, plan.md "Definition of Done", tasks.md T805
**Details**: SC-006 requires "every package ships a README + one example test". T805 verifies this at DoD time, but there is no generation task per package in Phase A–E. ~50 packages mean ~50 READMEs. Without distributed tasks, authoring will pile up at T805 and block the merge.
**Suggested Fix**: Add a README task to each package-creation task (e.g., "T040a: Write `src/Rig.TUnit.Databases/README.md` with public-API example"). Alternatively, add one bulk-write task per phase (e.g., T160b "Write READMEs for all Phase A packages") to keep authoring proximate to API definition.

---

### [MEDIUM] Naming Consistency: `UseRedis` builder method is ambiguous (cache vs KV roles)

**Location**: spec.md US2/US10, quickstart.md "Fluent builder entry point", data-model.md
**Details**: Quickstart shows `rig.UseRedis(RigConnect.FromContainer(redisFixture))` — but Redis now has two roles: (1) cache via `Rig.TUnit.Caching.Redis` (`RedisCacheRigBuilder`), (2) KV store via `Rig.TUnit.Databases.NoSql.Redis` (`RedisKvRigBuilder`). Both exist and share the underlying `RedisFixture`. The bare `UseRedis` method doesn't disambiguate which role the consumer wants. Docs and builder extensions may conflict.
**Suggested Fix**: Rename builder methods explicitly — `UseRedisCache(source)` (extends `CacheRigBuilder`) and `UseRedisKv(source)` (extends `NoSqlRigBuilder`). Deprecate/remove ambiguous `UseRedis` before Phase A merge. Update quickstart.md accordingly.

---

### [MEDIUM] Coverage Gaps: `RigBuilderExtensions` in Core needs updates per new base area

**Location**: data-model.md §"Builder Base Hierarchy", tasks.md
**Details**: `Rig.TUnit.Core/Builder/RigBuilderExtensions.cs` exists as the fluent-chain entry (from 002 feature). Each new base area needs its own `UseX` extension method that hands off to the area-specific builder. The tasks mention area-specific extensions (e.g., T063 for `SqlServerRigBuilderExtensions`) but do not have tasks ensuring the top-level `RigBuilderExtensions` in Core is updated with dispatching methods for new areas (`UseDatabases`, `UseMessaging`, `UseCaching`, `UseObservability`, `UseSecurity`, `UseStorage`).
**Suggested Fix**: Add a Phase A task after base packages ship: "Update `Rig.TUnit.Core/Builder/RigBuilderExtensions.cs` to expose `UseDatabases`, `UseMessaging`, `UseCaching` entry points returning the area's builder." Repeat for new Phase B–E area bases. Alternatively, each area base owns its own entry-point extension file on `RigBuilder`.

---

### [MEDIUM] Architecture Consistency: NetArchTest rule `EveryPublicType_HasReferencingTestAssembly` (T030) is implementation-ambiguous

**Location**: tasks.md T030, data-model.md §"Architecture Test Rules"
**Details**: NetArchTest.Rules analyzes one assembly's types against rules — it does not natively enumerate "this public type is referenced by some test assembly". Implementing T030 requires either (a) custom reflection across all test assemblies collecting `typeof()` references, or (b) a source-generator / Roslyn-analyzer approach. The task treats this as a standard NetArchTest rule; it is not.
**Suggested Fix**: Clarify T030 implementation approach in tasks.md. Recommend custom `ArchitectureTestBase` that loads all `Rig.TUnit.*.Tests.*.dll` assemblies and reflects over type usages; fall back to simpler heuristic (public type must match `{TypeName}Tests` convention) if reflection-based coverage is too brittle.

---

### [MEDIUM] Coverage Gaps: `ReplaceDbContext<TContext>` inheritance not called out

**Location**: data-model.md §"SqlRigBuilder<TSelf>", tasks.md T053
**Details**: The existing 002 feature introduced `ReplaceDbContext<TContext>()` on `SqlServerRigBuilder`. Plan promotes this to the `SqlRigBuilder<TSelf>` base so Sqlite and future providers inherit it. T053 creates `SqlRigBuilder<TSelf>` but the description doesn't enumerate `ReplaceDbContext<TContext>` — could be missed during implementation, forcing SqliteRigBuilder to reimplement.
**Suggested Fix**: Update T053 description to explicitly list the methods inherited/promoted: `ReplaceDbContext<TContext>()`, and note that SqlServerRigBuilder / SqliteRigBuilder override only provider-specific pieces.

---

### [MEDIUM] Coverage Gaps: Phase E does not include per-package contract tests for remaining SQL provider (Oracle)

**Location**: tasks.md T600, T710
**Details**: T600 creates `src/Rig.TUnit.Databases.Sql.Oracle/`. T710 blanket-covers "each new Phase E provider inherits its base's contract + ≥ 3 quirk tests". But Oracle has a dedicated Phase E task (T600) without a matching integration-tests task (e.g., there's no T601 "Create `tests/Rig.TUnit.Databases.Sql.Oracle.Tests.Integration/`"). Same pattern for T610 (Dynamo), T620 (Cassandra), T630 (EventStore), T640 (ElasticSearch), T650 (Sqs), T660 (Nats), T670 (MinIO), T680 (FileSystem) — each missing a paired test-project task.
**Suggested Fix**: Either (a) explicitly enumerate `Tests.Integration` tasks per Phase E provider (T601, T611, T621, T631, T641, T651, T661, T671, T681), or (b) make T710 more concrete with file paths for each provider's test project.

---

### [MEDIUM] Concurrency: Phase C `ConcurrencyAssert` cross-provider matrix incomplete until Phase D

**Location**: spec.md US9.1, plan.md Phase C §3, tasks.md T314, T450
**Details**: US9.1 says "`ConcurrencyAssert.TwoWriters(entity).OneWinsWith<DbUpdateConcurrencyException>()` run against SqlServer + Postgres + Cosmos + Mongo". But Postgres/Cosmos/Mongo providers don't ship until Phase D. T314 notes "Postgres + Cosmos + Mongo added in Phase D if available" and T450 explicitly handles the expansion. This is tracked — but the Phase C merge gate (T370) could be misread as requiring all 4 providers to pass. The gate description says "Concurrency contract GREEN on available providers" which is clearer but subtle.
**Suggested Fix**: Add a note to T370: "Concurrency contract runs against SqlServer only in Phase C; expands to Postgres/Cosmos/Mongo in Phase D (T450). US9.1 acceptance scenario is considered met at end of Phase D, not Phase C." Also cross-reference this in spec.md US9.

---

### [MEDIUM] Naming Consistency: `LoggingDetectorOptions.AdditionalPiiPatterns` regex format not specified

**Location**: spec.md FR-072, data-model.md §"Options Classes", tasks.md T223
**Details**: The option is typed `IReadOnlyList<string>`. Consumers must know whether patterns are raw regex (e.g., `"^x-auth-.*$"`) or glob (e.g., `"x-auth-*"`). Spec uses `^x-auth-.*$` (regex syntax) but doesn't assert the format. T223 doesn't specify. Inconsistent interpretation = runtime failures.
**Suggested Fix**: Add explicit XML doc on `AdditionalPiiPatterns` stating "ECMAScript regex patterns; compiled once at detector-startup; match is case-insensitive". Update T223 description.

---

### [MEDIUM] Architecture Consistency: Anti-pattern detector runtime mechanism unspecified

**Location**: spec.md FR-072, US5.2/US5.3, tasks.md T224, data-model.md
**Details**: The anti-pattern detector is described as "fails the test with a diagnostic referencing the offending call site" for interpolated templates and `Console.Write` from source assemblies. Two incompatible implementation approaches exist: (A) runtime inspection of captured `LogMessage.OriginalFormat` property (only works for log calls, not `Console.Write`, and only flags at test-time after the call) or (B) static source scanning via a Roslyn analyzer (catches at compile-time, covers `Console.Write`, but needs NuGet analyzer packaging). Plan and tasks do not pick a lane.
**Suggested Fix**: Add a clarification item in spec.md or research.md: "anti-pattern detection uses approach X (runtime / static / hybrid)". If runtime-only, spec should narrow FR-072 to log-call interpolation (drop `Console.Write`, since it won't be observable without static scanning unless the detector hooks `Console.SetOut`). Alternatively, ship both: a runtime detector in `.Logging` + a separate Roslyn analyzer in `Rig.TUnit.Observability.Logging.Analyzers` (add a Phase B task for the analyzer).

---

### [MEDIUM] Naming Consistency: Test-project naming `Tests.Contract` vs `Tests.Integration.Contract`

**Location**: tasks.md T049, T059, T084, T096, T129, T202, T444
**Details**: Contract test projects are named `Rig.TUnit.{Area}.Tests.Contract` throughout. But T151 places `ParallelIsolationContract` in `tests/Rig.TUnit.Parallelism.Tests.Contract/ParallelIsolationContract.cs (or Core.Tests.Contract until Parallelism package ships)`. Creates a mixed pattern where one contract class is sometimes in Parallelism.Tests.Contract and sometimes in Core.Tests.Contract.
**Suggested Fix**: Commit to a single home until `Rig.TUnit.Parallelism` ships (Phase E). Either (a) keep the abstract class in `Rig.TUnit.Core.Tests.Contract` from Phase A onward, then move to Parallelism.Tests.Contract during Phase E with a namespace-rename migration task; or (b) create `Rig.TUnit.Parallelism.Tests.Contract` early in Phase A as a stub project containing only this contract.

---

### [LOW] Coverage Gaps: No task for commit-message hook / PR template to enforce TDD cadence

**Location**: spec.md US1, plan.md §"TDD Execution Discipline", tasks.md phase 0
**Details**: Plan says commit-message prefixes (`test: red`, `feat: green`, `refactor:`) are "enforced by hook/reviewer". T809 verifies at DoD that "every commit exhibits cadence" but there's no proactive setup task. Manual reviewer discipline can drift; a commit-msg hook or GitHub Actions workflow would auto-enforce.
**Suggested Fix**: Add T006 (Phase 0) "Create `.githooks/commit-msg` that validates commit-message prefix matches `test: red|feat: green|refactor|chore|docs`; add `git config core.hooksPath .githooks` to README setup." Or add a GitHub Actions check in Phase 0 tooling.

---

### [LOW] Naming Consistency: `RigFixture` vs `RigFixtureBase` in quickstart.md

**Location**: quickstart.md "Your first test"
**Details**: Quickstart shows `public OrderHandlerTests(RigFixture rig)` and `_rig.Mediator`, `_rig.TenantId`. `RigFixture` is not defined anywhere — only `RigFixtureBase` (abstract, from Core) exists. This likely refers to a user-defined subclass that consumers build by extending `RigFixtureBase` + `CompositeFixture`, but the quickstart doesn't show that derivation.
**Suggested Fix**: Either add a preceding quickstart snippet showing `public sealed class RigFixture : CompositeFixture { ... }` (consumer-owned class), or use a more generic `TestContext rig` parameter. Current form could confuse new adopters.

---

### [LOW] Coverage Gaps: Benchmarks baseline migration not explicit

**Location**: plan.md "Benchmarks", tasks.md T720, SC-004
**Details**: SC-004 requires "< 110% of 002 baseline". T720 expands the benchmark suite per area. No task captures the pre-cutover 002 baseline measurements or stores them in the repo as a reference JSON. After hard cutover, the baseline is irrecoverable.
**Suggested Fix**: Add a Phase 0 task "T007: Run `dotnet test tests/Rig.TUnit.Benchmarks -c Release --filter *FixtureStartup*` before any cutover work; commit `benchmarks/baseline-002.json` as reference." T720 compares against this file.

---

### [LOW] Architecture Consistency: `Rig.TUnit.All` meta-package has no architecture test preventing circular refs

**Location**: spec.md FR-112, tasks.md T702
**Details**: `Rig.TUnit.All` references every package. If architecture tests are distributed across test assemblies, `Rig.TUnit.All.Tests.Unit` (doesn't exist) would have 50 references. The current rules (T021–T030) operate at individual package granularity. `Rig.TUnit.All` is a pure meta-package with no source types, so most rules are trivial — but there is no explicit rule verifying it is a pure meta (only `PackageReference` entries, no source `.cs`).
**Suggested Fix**: Add a rule (T702a or addition to T030): "Rig.TUnit.All MUST contain zero source files; verify by asserting `Rig.TUnit.All.dll` is empty (only forwarded types)." Low priority because the current structure makes accidental source inclusion unlikely.

---

### [LOW] Naming Consistency: `Rig.TUnit` meta-package composition unclear in spec vs plan vs quickstart

**Location**: spec.md FR-110, plan.md, quickstart.md
**Details**: FR-110 says `Rig.TUnit` bundles "Core + Mediator + Grpc + WebAPI + common". Plan's architecture diagram shows same. Quickstart shows `<PackageReference Include="Rig.TUnit" />` then advises supplementing with `Rig.TUnit.Databases.Sql.SqlServer` and `Rig.TUnit.Security.Jwt` — implying `Rig.TUnit` does NOT include databases. Fine as-is but "common" in FR-110 is undefined.
**Suggested Fix**: Define "common" explicitly in FR-110 (e.g., "common = Core + Mediator + Grpc + WebAPI; no provider packages") or remove the vague term.

---

## Traceability Matrix (summary)

| Source | Count | Task Coverage | Notes |
|---|---|---|---|
| Functional Requirements (FR) | ~50 (FR-001..FR-120) | 100% traced | `[FR:###]` tags on tasks |
| User Stories (US) | 13 | 100% traced | Phase A → US1/US2/US3/US4/US13; Phase B → US5/US6/US7; Phase C → US8/US9/US10; Phase D → US11; Phase E → US12 |
| Clarifications (C-001..C-005) | 5 | 100% traced | C-001 in T131; C-002 in T701; C-003 in T360–T365; C-004 in T042; C-005 in T223 |
| Hard Requirements (R1..R10) | 10 | 100% traced | via Traceability table in spec.md + `[FR:###]` in tasks |
| Success Criteria (SC) | 15 | 100% traced | Phase F (T800–T812) + distributed merge gates |

---

## Overall Assessment

**No CRITICAL blockers.** All 17 findings (3 HIGH + 8 MEDIUM + 6 LOW) resolved in the 2026-04-17 fix pass.

Artifacts updated:
- **spec.md**: C-006 added (anti-pattern detector mechanism); FR-110 definition of "common" removed; US9.1 Phase-C vs Phase-D note added.
- **plan.md**: package tree shows `.Logging.Analyzers`; Definition-of-Done entry references per-phase README tasks.
- **tasks.md**: 338 tasks (up from 312) — 26 new tasks (T006, T007, T159, T227, T228, T289, T369, T469, T601, T611, T621, T631, T641, T651, T661, T671, T681, T703, T719, plus updated descriptions on ~15 existing tasks).
- **data-model.md**: `IsolationKey` pinned to Core; Redis builder dual-role pattern documented; `ReplaceDbContext<TContext>` inheritance made explicit; anti-pattern detector mechanism section added; per-area `RigBuilderExtensions` convention documented.
- **quickstart.md**: proper `MyTestRig : CompositeFixture` derivation example; `UseRedisCache` / `UseRedisKv` disambiguation.

---

## Recommended Next Steps

1. **Proceed to `/dai.go`** — no blockers remain. Start with Phase 0 (T001–T007).
2. **At each phase merge gate**: re-run `/dai.analyze` to catch drift.
3. **During implementation**: resolve the 6 "Open research items" in research.md inline as encountered (R1–R6). None block planning.

Ready for `/dai.go`.
