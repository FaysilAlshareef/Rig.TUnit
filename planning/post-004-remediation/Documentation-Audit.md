# Documentation Audit

**Date:** 2026-04-19 (revised 2026-04-19 — quality-based, replacing the original size-based audit)
**Scope:** root governance docs, per-project READMEs, planning/feature docs, contributor onboarding.

> **Revision note:** the original audit ranked READMEs by byte count (EXCELLENT > 2000 B, GOOD 500–2000 B, MINIMAL 100–500 B, MISSING) and recommended only fixing the 12 missing + 2 minimal ones. Repo owner reviewed the so-called "best" READMEs (`Databases.Sql.MySql`, `Microservices.Outbox`) and confirmed that **size is not the gate — quality is**. Even the 2 EXCELLENT READMEs miss critical sections (Options table, API surface, parallel-isolation semantics, troubleshooting, version-compat matrix). This document now treats **all 63 src projects** as needing a rewrite against a single canonical template, and tightens the architecture rule from a character-count check to a structural section-presence check.

## 1. Solution-level — score 4/10

Root [`README.md`](../../README.md) is 22 lines. Coverage limited to:
- One-line description
- `git config core.hooksPath .githooks` setup
- Folder layout note

### Missing root governance files

| File | Status | Priority |
|---|---|---|
| `LICENSE` | Absent | P0 (blocks OSS release) |
| `CONTRIBUTING.md` | Absent (only `src/Rig.TUnit/Contributing-ProviderTemplate.md` buried deep) | P0 |
| `SECURITY.md` | Absent | P0 |
| `CHANGELOG.md` | Absent | P1 |
| `CODE_OF_CONDUCT.md` | Absent | P2 |

### Missing root README sections

- Project purpose beyond the one-liner
- Feature matrix (what families, which providers)
- Architecture overview / diagram
- Install / quick-start
- Links to per-provider READMEs
- Links to `Contributing-ProviderTemplate.md`
- API stability / versioning policy
- Roadmap and vision
- Supported .NET versions (currently implied by `global.json` / `Directory.Build.props`)
- CI status badge + coverage badge
- Link to the `.dotnet-ai-kit/features/` SDD history

## 2. Per-project READMEs — quality verdict

### Coverage state

| State | Count | Notes |
|---|---|---|
| MISSING | 12 | Listed in §2.1 |
| PRESENT but below the new quality bar | 51 | Includes the previously-EXCELLENT MySql and Outbox |
| MEETS the new quality bar | 0 | None of the current READMEs satisfy all 14 mandatory sections |

**Conclusion:** All 63 src projects need a README produced or rewritten against the canonical template in §3.

### 2.1 12 src projects missing README

- `src/Rig.TUnit/` (entry-point package)
- `src/Rig.TUnit.All/` (meta-package)
- `src/Rig.TUnit.Ci/`
- `src/Rig.TUnit.Core/`
- `src/Rig.TUnit.Grpc/`
- `src/Rig.TUnit.Mediator/`
- `src/Rig.TUnit.Microservices/` (base)
- `src/Rig.TUnit.Microservices.Contracts/`
- `src/Rig.TUnit.Microservices.Saga/`
- `src/Rig.TUnit.Parallelism/`
- `src/Rig.TUnit.Storage/` (base)
- `src/Rig.TUnit.WebAPI/`

### 2.2 51 existing READMEs — quality gaps

Documentation Owner audited four representative READMEs in detail to confirm the size-vs-quality mismatch. The findings generalise across the remaining 47.

#### `Databases.Sql.MySql/README.md` — previously rated EXCELLENT (2104 B)

Has: purpose, install, two quick-starts, options table.
**Missing:** `Helpers/` API surface, `SectionName` constant, appsettings binding example, parallel-isolation semantics (IsolationKey), `SqlRigContract` link, troubleshooting (Docker daemon, port conflicts, MySQL slow-startup), Pomelo EF Core 10 version-compat matrix, benchmark class location + baseline numbers, badges, contributing link, spec / FR reference, when-to-use vs `SqlServer` / `Postgres` / `Sqlite`, when NOT to use.

#### `Microservices.Outbox/README.md` — previously rated EXCELLENT (2034 B)

Has: purpose, install, two examples, spec line.
**Missing:** Options table, complete API surface (`OutboxFixture` / `OutboxRelaySimulator` / `OutboxAssert` / `OutboxReplay` / `OutboxSchema` / `InMemoryOutboxStore` / `CustomOutboxStore<TRow>` each need a one-liner), exactly-once semantics explained, failure modes (CAS contention, poison messages, duplicate publish), when-to-use vs `Inbox` / `EventSourcing`, troubleshooting, performance characteristics, badges, contributing link.

#### `Caching.Redis/README.md` — previously rated GOOD (908 B)

Has: install, one-liner example, three-word dependencies list.
**Missing:** complete Options table, `SectionName`, `RedisBackplaneCapture` API + use case, when-to-use vs `Caching.Memory` (in-process vs networked), when-to-use vs `Databases.NoSql.Redis` (cache vs KV role — explicit non-bare-`UseRedis` design), how IsolationKey applies to a shared container, cache-invalidation test pattern, troubleshooting Docker, performance numbers, parallel-isolation behaviour for shared `RedisFixture`.

#### `Messaging/README.md` — previously rated MINIMAL (474 B)

Has: list of base types.
**Missing:** purpose explanation, what `IMessagingRig` actually contracts, base-class extension story, when a consumer would touch this base directly vs a leaf provider, link to each leaf provider, options story for the base, contributing link, role of W3C traceparent propagation, listener/sender lifecycle.

### 2.3 Generalisation — failure modes that recur across READMEs

| # | Failure mode | Occurrences | Why it matters |
|---|---|---|---|
| 1 | No "When to use vs alternatives" section | ~50 of 51 | Users pick the wrong provider (Redis-cache vs Redis-KV, MinIO vs S3, ServiceBus vs Kafka, Memory-cache vs Hybrid) |
| 2 | No "When NOT to use" / non-goals | ~51 of 51 | Misuse → bug reports the package can't honour |
| 3 | Options table absent or partial | ~45 of 51 | `SectionName`, defaults, validation, env binding all undocumented |
| 4 | API surface not enumerated | ~50 of 51 | Users discover types via IntelliSense or reading source — slow + error-prone |
| 5 | No parallel-isolation semantics | ~51 of 51 | IsolationKey contract, fixture sharing, `[NotInParallel]` guidance — invisible |
| 6 | No provider quirks documented | ~40 of 51 | AUTO_INCREMENT, RU charges, PL/SQL timing, keyspace lifecycle, stream semantics — surprise users in prod-like tests |
| 7 | No troubleshooting | ~51 of 51 | Container timeout, Docker daemon down, port conflicts, image pull failures — first-hour pain |
| 8 | No version-compat matrix | ~50 of 51 | EF Core 10 + Pomelo, Testcontainers minimums, `dotnet sdk` floor — silently break |
| 9 | No spec / FR reference | ~30 of 51 | Cannot trace a behaviour back to the requirement that drove it |
| 10 | No badges (NuGet / CI / coverage) | 51 of 51 | Discoverability + trust signal absent |
| 11 | No contributing link | 51 of 51 | New-contributor funnel broken |
| 12 | No related-packages map | ~51 of 51 | Family siblings + cross-family connectors not surfaced |

## 3. Canonical README template — 14 mandatory sections

Every package README MUST contain these 14 sections (or an explicit "N/A — base/meta package, see §3.2" rationale where applicable). This replaces the prior "≥ 100 chars" floor with a structural gate.

### 3.1 Section list

| # | Section heading | Required content |
|---|---|---|
| 1 | _(top of file — badges)_ | NuGet version, downloads, CI status, coverage, license — Markdown badge syntax |
| 2 | `## Purpose & value` | One paragraph: what this is, what problem it solves, when to pick it vs alternatives in the same family |
| 3 | `## When NOT to use` | Explicit non-goals so users self-select out (e.g., "Use `Caching.Memory` for in-process tests; this package starts a Docker container") |
| 4 | `## Install` | `dotnet add package …`, `<PackageReference />`, supported TFM, Docker / OS prereqs, version-compat matrix |
| 5 | `## Quick start` | Runnable `[Test]` end-to-end with `using` imports + teardown — copy-paste ready |
| 6 | `## Configuration` | Full Options table (Property / Type / Default / Required? / Validation / Purpose), `SectionName` constant, appsettings.json example, programmatic override example, environment-variable binding example |
| 7 | `## API surface` | Every public type with one-line purpose: Fixture, Options, RigBuilder, Use{Provider} extension, Helpers, Assertions, custom DSL |
| 8 | `## Fluent wiring` | `RigBuilder.Use{Provider}(...)`, `RigConnect.FromContainer / FromValue / FromEnvironment` semantics, IsolationKey behaviour, CancellationToken, disposal contract |
| 9 | `## Provider quirks` | Surprising defaults, workarounds, known limitations (AUTO_INCREMENT, RU charges, PL/SQL timing, keyspace-per-test, stream semantics, Pomelo EF10 pin) |
| 10 | `## Troubleshooting` | Container startup timeouts, Docker daemon issues, port conflicts, credential errors, image pull failures, recommended timeout tuning |
| 11 | `## Testing contracts` | Which `{Family}RigContract` + `ParallelIsolationContract<T>` is inherited; how to add `*QuirkTests.cs` |
| 12 | `## Performance` | BenchmarkDotNet class location under `tests/Rig.TUnit.Benchmarks/`, baseline numbers (cold container start, warm op, 100-op throughput), measurement caveats |
| 13 | `## Dependencies & related packages` | Direct NuGet deps with hyperlinks, upstream container image + tag link, family siblings list, cross-family connectors, meta-packages |
| 14 | `## Spec, versioning, contributing` | FR IDs (e.g., FR:090, FR:093) + feature folder link, API stability tier (Stable / Preview / Experimental), breaking-change history (notably KurrentDb rename in 004), link to root `CONTRIBUTING.md` and `Contributing-ProviderTemplate.md` |

### 3.2 Variant for base / meta packages

Sections 9, 10, 12 may be marked "N/A — abstract base package" or "N/A — meta-package" with a one-line rationale, NEVER omitted. The remaining 11 sections are mandatory.

For meta-packages (`Rig.TUnit.All`, `Rig.TUnit.Microservices`):
- Section 5 (Quick start) shows the meta-include shortening N package references to one
- Section 7 (API surface) lists every transitively-included leaf package with one-line purpose
- Section 13 (Dependencies) is the full transitive list — this is the meta-package's headline value

### 3.3 Stored at

`docs/templates/PROVIDER_README_TEMPLATE.md` — single source of truth, referenced by:
- The hardened `ReadmeCompletenessTests` (parses headings, asserts presence)
- Root `CONTRIBUTING.md` (linked for new-provider authors)
- This audit document (linked from §3.1)

A second file `docs/QUALITY-BAR.md` ships the human-reviewer rubric: each of the 14 sections graded Pass / Needs-work / Missing, with examples of each.

## 4. `Contributing-ProviderTemplate.md` — EXCELLENT but mis-located

[`src/Rig.TUnit/Contributing-ProviderTemplate.md`](../../src/Rig.TUnit/Contributing-ProviderTemplate.md) exists and is production-quality:
- Full canonical file layout
- 7+ copy-paste-ready code examples
- Links to 3 canonical providers
- Architecture-test assertions referenced
- Options validation patterns shown

**Issues:**
1. Buried under `src/Rig.TUnit/` rather than linked from the root README or a root `CONTRIBUTING.md`. New contributors will not find it.
2. Section 8 ("README.md") still describes the OLD floor (`> 100 chars`) — must be updated to reference the 14-section canonical template once Phase 6 lands.

## 5. Planning / feature docs (`.dotnet-ai-kit/features/`)

Four feature folders exist:

| Feature | Title | Status |
|---|---|---|
| 001 | Rig.TUnit Testing Infrastructure Library | Historical (shipped) |
| 002 | Rig.TUnit Fluent Builder Expansion | Historical (shipped) |
| 003 | Rig.TUnit Ecosystem Expansion | Historical (shipped) |
| 004 | Rig.TUnit Provider Consistency Remediation | Merged 2026-04-18 (PR #3) |

Plus `planning/` folders covering the same four features (this folder becomes the 5th).

**Classification:** these are internal spec-driven-development artefacts, not consumer-facing docs. FR IDs (e.g., "FR-030") and task IDs (e.g., "T172") are not explained in public docs — they presume SDD context. Value for library users is minimal — but READMEs section 14 will surface FR references back to the spec folder for traceability.

## 6. Architecture-rule tightening

`tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` currently asserts `> 100 chars`. After Phase 6 lands the rule MUST tighten to:

1. Parse Markdown headings via `Markdig` (already a transitive dep candidate) or a regex-based fallback
2. Assert presence of all 14 section headings from §3.1 OR an explicit `## §N — N/A: <rationale>` line
3. For Section 6, additionally assert the Options table contains at least the rows expected for the package's known options (read from the matching `*FixtureOptions.cs` via reflection)
4. For Section 12, assert the linked benchmark class exists in `tests/Rig.TUnit.Benchmarks/`
5. Skip-list policy: zero entries — every package must conform

This converts `ReadmeCompletenessTests` from a typo-catcher into a real documentation gate.

## 7. Recommendations by priority

### P0 — Block OSS release

1. Add `LICENSE` (proposed: MIT, match typical .NET OSS library)
2. Add `CONTRIBUTING.md` at root — top-level TDD/commit rules + link to `Contributing-ProviderTemplate.md` + link to the canonical README template
3. Add `SECURITY.md` at root — vulnerability disclosure channel
4. Author `docs/templates/PROVIDER_README_TEMPLATE.md` and `docs/QUALITY-BAR.md` — these unblock Phase 6 work
5. Rewrite root `README.md` against an adapted 14-section template (feature matrix replaces "API surface", ecosystem map replaces "Provider quirks")

### P1 — Quality bar — High impact

6. **Rewrite all 63 per-project READMEs against the canonical template** — staged per family (SQL, NoSQL, Caching, Messaging, Microservices, Security, Observability, Storage, Core/utility, meta)
7. Add `CHANGELOG.md` documenting 001–004 releases with breaking changes (notably the EventStore → KurrentDb rename in 004)
8. Tighten `ReadmeCompletenessTests` to enforce the 14-section structure (§6) — lands as the LAST step after every README conforms
9. Update Section 8 of `Contributing-ProviderTemplate.md` to point at the new canonical template

### P2 — Nice-to-have but materially raise the docs ceiling

10. Architecture Decision Records under `docs/adr/` — minimum 8:
   - ADR-001: Why Testcontainers over Docker Compose primary
   - ADR-002: Why CRTP `RigBuilder<TSelf>` pattern
   - ADR-003: Why Options pattern with `SectionName` (vs `IConfiguration` injection)
   - ADR-004: Why TUnit over xUnit / NUnit / MSTest
   - ADR-005: Why family-level contract tests over per-provider contract files
   - ADR-006: Why IsolationKey over static state for parallel safety
   - ADR-007: Why explicit `UseRedisCache` / `UseRedisKv` instead of bare `UseRedis`
   - ADR-008: KurrentDB rename (Feature 004 Phase 1) — breaking change rationale
11. Architecture diagram (Mermaid) showing family graph + 60-provider matrix — embedded in root README + linked from each leaf README's section 13
12. Troubleshooting guide: container startup timeouts, network conflicts, Docker daemon issues, image pull failures (consolidated under `docs/troubleshooting.md` — leaf READMEs link to provider-specific subsections)
13. Glossary under `docs/glossary.md` — every term used in any README MUST resolve here: Fixture, Rig, Contract, Stampede, Backplane, IsolationKey, Sender, Listener, RigConnect, ParallelIsolationContract, QuirkTests, EventSender, OutboxRelaySimulator, etc.
14. Performance tuning guide: when to use which cache / storage / db provider for which test scenario
15. Migration guide: version upgrade path between 001 → 002 → 003 → 004 (notably KurrentDb)

## 8. Effort estimate — quality-driven

The original audit estimated 25–35 h based on size-only fixes. The new quality bar materially expands per-README work because each README now needs provider-specific research (quirks, version-compat, baseline numbers).

| Item | Original estimate | Revised estimate | Note |
|---|---|---|---|
| Root README + LICENSE + CONTRIBUTING + SECURITY | 4–6 h | **8–12 h** | Root README jumps from 22 lines to a 14-section canonical document |
| Authoring `PROVIDER_README_TEMPLATE.md` + `QUALITY-BAR.md` | — | **3–4 h** | New deliverable, unblocks all 63 rewrites |
| 63 READMEs rewritten against template | 7–9 h (12 new + 2 expand) | **50–70 h** | 45–90 min per README; per-provider research; quirks captured |
| `CHANGELOG.md` covering 001–004 | 2–3 h | 3–4 h | Includes KurrentDb breaking-change narrative |
| 8 ADRs (was 6) | 4–6 h | **6–8 h** | Two extra ADRs (IsolationKey, Redis split) |
| Architecture diagram + feature matrix | 2 h | 4 h | Mermaid + linked from every leaf README |
| Glossary + troubleshooting + tuning + migration guides | 4 h | **6–8 h** | All four guides — glossary becomes mandatory because READMEs reference terms |
| Tighten `ReadmeCompletenessTests` (parse headings + Options-table check + benchmark-link check) | 15 min | **3–4 h** | Real implementation, not a string-length bump |
| **Total** | **~25–35 h** | **~80–110 h (~10–14 working days)** | |

This is enough scope that Phase 6 of the Feature 005 roadmap likely becomes its own sub-feature `005-b-docs-parity` running in parallel with the test fill-in work, rather than a sequential phase.

## 9. Acceptance criteria for "documentation done"

A README is **done** when all of:

- All 14 sections from §3.1 present (or `## §N — N/A: <rationale>` for base/meta packages per §3.2)
- Options table mirrors the matching `*FixtureOptions.cs` exactly (property names, defaults, required flags)
- Quick start compiles when copy-pasted into a fresh `[Test]` (verified by an arch-test snippet-extraction step in the future)
- Every type listed in API Surface exists in the package (verified by reflection)
- Every link resolves (verified by a Markdown link-checker step in CI)
- Provider quirks section names at least one provider-specific surprise (e.g., MySql AUTO_INCREMENT, Cosmos RU)
- Spec reference points at a real folder under `.dotnet-ai-kit/features/`
- File passes the hardened `ReadmeCompletenessTests`

Documentation is **complete** when:

- All 63 src READMEs meet the per-file acceptance criteria above
- Root `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `LICENSE`, `CHANGELOG.md` all present and rewritten against P0 directive
- 8 ADRs published under `docs/adr/`
- `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md` all present
- `ReadmeCompletenessTests` tightened to structural gate with zero `SkipUntilFixed` markers
- One green CI run after the gate is tightened
