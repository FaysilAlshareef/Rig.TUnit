# Changelog

All notable changes to Rig.TUnit are documented in this file. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added — Feature 005 (Legacy Coverage & Docs Parity)
- MTP-native `--coverage` collection on every `integration-*` CI job (FR-020).
- `coverage-summary` CI job merging cobertura → Html + Markdown via ReportGenerator,
  publishing to `$GITHUB_STEP_SUMMARY`, and uploading a 30-day `coverage-report` artefact (FR-021).
- Per-package coverage threshold gate (`line-rate ≥ 0.90`, `branch-rate ≥ 0.85`) enforced
  on every PR (FR-022 — non-blocking in Phase 2, blocking from T069b).
- `benchmarks/coverage-baseline-005.json` + `benchmarks/baseline-005.json` schemas (FR-023, FR-037).
- Missing test categories filled for pre-004 providers:
  - Integration + Contract for Rig.TUnit.Core (T021).
  - Integration + Contract + Benchmark for Rig.TUnit.Mediator, Grpc, WebAPI, Http (T023-T029).
  - Unit and/or Benchmark for Rig.TUnit.Ci, Concurrency, HealthChecks, Parallelism, Resilience
    (T031, T033, T035, T037, T039).
  - Unit and/or Benchmark for Caching.Memory/Redis, Databases.Sql.Sqlite/SqlServer,
    Databases.NoSql.Redis (T041, T043, T045, T047, T049).
  - Unit and Benchmark for Observability.Logging/Seq/Tracing (T051, T053, T055).
  - Unit and Benchmark for Microservices.{Contracts, Saga, Inbox, Outbox, Snapshots}
    (T057-T065).
- `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` — per-test physical database
  isolation for Postgres tests, resolving the master-CI flake (T004, FR-010, SC-001).
- `OrphanFolderTests` — regression guard for pre-004 path deletions (T002, FR-012).
- `ArtifactUploadTests` + per-job `upload-artifact@v4` step with 14-day retention (FR-013).
- `CoverageCollectionTests`, `CoverageSummaryJobTests`, `CoverageThresholdTests` — YAML
  assertions enforcing the coverage pipeline shape.
- `NoSkipMarkersTests` + `SharedFixtureGuardTests` — enforcement rules that prevent
  regression of the FR-004 / FR-011 invariants (T104b/T104c, SC-012/SC-013).
- Phase-1 minimal `commit-discipline-gate` CI job checking RED→GREEN subject pairing
  on every PR (partial FR-002; full hardening in Phase 7).
- Root governance files: `LICENSE` (MIT), `SECURITY.md`, `CHANGELOG.md` (this file),
  updated `CONTRIBUTING.md`, rewritten `README.md` (T121, FR-060).

### Changed
- `UsePostgresFluentTests` now acquires per-test ephemeral databases — every test owns
  its physical DB, so schema-inspection assertions are deterministic under parallel execution.
- Every `integration-*` matrix job's `dotnet test` invocation gains
  `-- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`.
- `Shared*Fixture.cs` files across 20 test projects gain an
  `Intentional reuse per A005 audit` rationale comment (FR-011).

### Deprecated
- Consumer projects using `coverlet.msbuild` should migrate to the MTP-native
  `--coverage` collector. The package pin is retained in
  `Directory.Packages.props` only for backwards compatibility with scripts that haven't
  migrated yet. See `CONTRIBUTING.md` § Coverage gate.

### Removed
- Orphan folders `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`,
  `tests/Rig.TUnit.SqlServer.Tests.Integration/` (T002 — already empty of tracked content;
  OrphanFolderTests enforces they stay gone).

### Fixed
- Postgres integration flake on master CI — `42P01: relation "Samples" does not exist`
  under parallel test execution (FR-010, SC-001, T003/T004).

## [0.4.0] — 2026-04-19 — Feature 004 (Provider Consistency Remediation)
- Canonical quartet (Fixture + FixtureOptions + RigBuilder + Use{Provider}) enforced
  across 32 providers via `ProviderCompletenessTests`.
- Test-pyramid completeness (`Unit + Integration + Contract + Benchmark`) per leaf
  provider via `TestCompletenessTests` (FR-031 onwards).
- `DatabasePerTestHelper` pattern established for SQL providers.

## [0.3.0] — 2026-03 — Feature 003 (Ecosystem Expansion)
- Hard cutover to the `Rig.TUnit.{Family}.{Provider}` naming convention.
- 14 new base + provider packages added across Databases, Messaging, Caching,
  Storage, Security, and Observability families.
- **Breaking rename:** `EventStore.Client.Grpc.Streams` → `KurrentDB.Client` per the
  Kurrent rebrand. `Rig.TUnit.Databases.NoSql.EventStore` is retired; consumers migrate
  to `Rig.TUnit.Databases.NoSql.KurrentDb`. Scripts that pinned the old NuGet package
  must update. See
  [planning/post-004-remediation/Documentation-Audit.md](planning/post-004-remediation/Documentation-Audit.md)
  for the migration log.

## [0.2.0] — 2026-02 — Feature 002 (Fluent Builder Expansion)
- `Rig.TUnit.WebAPI` package with `WebApplicationFactory`-backed `HttpClientHelper`.
- Test-authentication helpers for bearer-token scenarios.
- All provider RigBuilders unified under the CRTP `{Family}RigBuilder<TSelf>` shape.

## [0.1.0] — 2026-01 — Feature 001 (Rig.TUnit Library)
- Initial release: `Rig.TUnit.Core` with `RigBuilder`, `RigConnect`, `IsolationKey`.
- First container-backed fixtures for SqlServer + Redis.

[Unreleased]: https://github.com/FaysilAlshareef/Rig.TUnit/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.4.0
[0.3.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.3.0
[0.2.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.2.0
[0.1.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.1.0
