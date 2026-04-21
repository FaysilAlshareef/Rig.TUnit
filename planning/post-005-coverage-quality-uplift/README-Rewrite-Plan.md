# README Rewrite Plan — Feature 006

**Audit date**: 2026-04-21  
**Problem**: The root `README.md` contains only brief bullet notes; it does not describe the
ecosystem, the provider model, the builder API, installation, or CI status.  It is the primary
landing page for the library on GitHub/NuGet.

---

## Target structure (14 sections)

Each section below states the heading, required content, and source of truth for that content.

---

### Section 1 — Headline + badges

**Heading**: *(none — top of file)*

**Content**:
- One-line description: "TUnit-first integration-testing rig for .NET 10 — 63 packages, zero
  boilerplate container setup."
- Badge row: NuGet version · CI status · Coverage percentage · License

**Source of truth**: `Rig.TUnit.slnx`; NuGet package IDs from `Directory.Build.props`;
coverage badge from GitHub Pages output (Task T043 / T080).

---

### Section 2 — What is Rig.TUnit?

**Heading**: `## What is Rig.TUnit?`

**Content**:
- 3–4 sentences: TUnit-first, provider pattern, 63 source packages, covers databases / messaging /
  storage / observability / security / microservices patterns.
- Bullet list of top-level family names with one-line descriptions.

**Source of truth**: `src/` directory structure; `PACKAGES.md` if it exists.

---

### Section 3 — Package families

**Heading**: `## Package families`

**Content**:
- Table with columns: Family | NuGet prefix | Providers | Purpose
- Rows: Core, Databases.Sql, Databases.NoSql, Caching, Messaging, Storage, Observability,
  Security, Microservices, Docker, Parallelism, Resilience, HealthChecks, Grpc, Http, WebAPI,
  Mediator, Ci

**Source of truth**: `src/` subdirectories; each provider's `.csproj` `<PackageId>`.

---

### Section 4 — Quick start

**Heading**: `## Quick start`

**Content**:
- `dotnet add package Rig.TUnit.Core` installation line.
- Minimal test class (20–30 lines) showing `RigConnect`, a fixture, and one assertion.
- Must compile against the latest release.

**Source of truth**: `tests/Rig.TUnit.Core.Tests.Unit/` for the minimal pattern;
`ConcurrencyBenchmarks.cs` for the style.

---

### Section 5 — Builder API

**Heading**: `## Builder API`

**Content**:
- Short prose explaining the canonical quartet: `{Provider}Fixture`, `{Provider}FixtureOptions`,
  `{Provider}RigBuilder`, `{Provider}RigBuilderExtensions`.
- Table of `RigConnect` factory methods: `FromContainer()`, `FromConfig()`, `FromOptions()`,
  `FromValue()`, `Auto()`.
- Code snippet showing `RigConnect.Auto()` vs `RigConnect.FromContainer()`.

**Source of truth**: `src/Rig.TUnit.Core/Builder/RigConnect.cs`;
`src/Rig.TUnit.Databases.Sql.Postgresql/Builder/PostgresRigBuilder.cs` as reference.

---

### Section 6 — IsolationKey

**Heading**: `## Test isolation`

**Content**:
- Explain `IsolationKey` as the per-test deterministic token.
- Show `IsolationKey.FromExecutionContext()` and the derived helpers
  (`ForDockerContainer()`, `ForPostgresDatabase()`, etc.).
- Explain why shared fixtures are safe when combined with `IsolationKey`.

**Source of truth**: `src/Rig.TUnit.Core/IsolationKey.cs`.

---

### Section 7 — Provider catalogue

**Heading**: `## Provider catalogue`

**Content**:
- One table row per provider: Package name | Container / service | Key helper classes | Link to
  provider README (if any).
- 63 rows; generated from `src/` directory names.

**Source of truth**: `src/` directory listing.

---

### Section 8 — Running the tests

**Heading**: `## Running the tests`

**Content**:
- TUnit uses Microsoft.Testing.Platform; `dotnet run --project` for local runs.
- Coverage command: `dotnet test --project <path> -- --coverage --coverage-output-format
  cobertura --coverage-output coverage.cobertura.xml`
- Note about Docker being required for integration tests.
- Link to `ci/coverage-scan` branch for full coverage report.

**Source of truth**: `tests/` README if it exists; `.github/workflows/ci.yml` step bodies.

---

### Section 9 — Benchmarks

**Heading**: `## Benchmarks`

**Content**:
- Link to benchmark dashboard (GitHub Pages, once T043 is complete).
- How to run locally: `dotnet run --project tests/Rig.TUnit.Benchmarks -c Release`.
- Note that `InProcessEmitBenchmarkConfig` uses `Job.Dry` / 1-iteration warm-up (smoke mode).

**Source of truth**: `tests/Rig.TUnit.Benchmarks/`; `Benchmark-Remediation-Plan.md`.

---

### Section 10 — CI / CD

**Heading**: `## CI`

**Content**:
- Matrix overview: SQL (6 providers), NoSQL (7), Caching (4), Messaging (6), Microservices (5),
  Security (4), Observability (6), Storage (5), Core (11 areas).
- Coverage gates: ≥ 90 % line, ≥ 85 % branch per package.
- Badge linking to latest CI run.

**Source of truth**: `.github/workflows/ci.yml`.

---

### Section 11 — TDD discipline

**Heading**: `## TDD discipline`

**Content**:
- RED commit (test only, must fail) → GREEN commit (source + passing tests).
- `commit-discipline-gate` job enforces this on every PR.
- Reference to `.dotnet-ai-kit/features/` for spec-driven development.

**Source of truth**: `ci.yml` `commit-discipline-gate` job; `planning/` README files.

---

### Section 12 — Contributing

**Heading**: `## Contributing`

**Content**:
- Branch naming: `feat/NNN-name`, `fix/NNN-name`, `chore/NNN-name`.
- Task to add: canonical quartet, unit tests, integration test, contract test, benchmark.
- Reference to `planning/` for feature roadmaps.

**Source of truth**: `planning/post-004-remediation/Proposed-Feature-005-Roadmap.md` — the
"Delivery mode" section describes the discipline.

---

### Section 13 — Architecture

**Heading**: `## Architecture`

**Content**:
- Diagram (Mermaid) showing the layer dependency: Core → Family base → Provider → Test.
- Explain that provider packages never depend on each other.
- Explain `Rig.TUnit.Ci` as a meta-package used only by CI workflows.

**Source of truth**: `src/` `.csproj` `<ProjectReference>` chains.

---

### Section 14 — License

**Heading**: `## License`

**Content**: License name + link to `LICENSE` file.

---

## Delivery

| Task | Description | Effort |
|------|-------------|--------|
| T060 | Draft Sections 1–4 (headline, what-is, families, quick-start) | 2 h |
| T061 | Draft Sections 5–7 (builder API, IsolationKey, provider catalogue) | 3 h |
| T062 | Draft Sections 8–11 (running tests, benchmarks, CI, TDD) | 2 h |
| T063 | Draft Sections 12–14 (contributing, architecture diagram, license) | 2 h |
| T064 | Review pass — compile all code snippets, verify all NuGet package names | 2 h |
| T065 | Merge to `master` after Phase 4 exit gate passes | — |

**Total estimated effort**: 11 h

---

## Non-goals

- Per-provider README files (out of scope for Feature 006; tracked separately).
- Versioned docs site (Docusaurus / MkDocs) — Phase 7+ consideration.
- Localisation of README content.
