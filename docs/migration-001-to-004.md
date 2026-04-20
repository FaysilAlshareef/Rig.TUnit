# Migration Guide — Rig.TUnit 0.1.0 → 0.4.x

Upgrade path for consumers coming from earlier Rig.TUnit versions.

## 0.1.0 → 0.2.0 (Feature 002)

### Added
- `Rig.TUnit.WebAPI` — `HttpClientHelper<TProgram>` + test-authentication helpers.

### Changed
- `RigBuilder` gained a CRTP base (ADR-002). Consumer code using
  `RigBuilder builder = …` still works; the concrete type returned by
  `services.AddRigTUnit(…)` is unchanged.

### Migration steps
Add the WebAPI package to any project standing up `WebApplicationFactory`:

```diff
+ <PackageReference Include="Rig.TUnit.WebAPI" />
```

## 0.2.0 → 0.3.0 (Feature 003) — HARD CUTOVER

### Breaking renames
- `Rig.TUnit.Databases.NoSql.EventStore` → **`Rig.TUnit.Databases.NoSql.KurrentDb`**
  (see [ADR-008](adr/ADR-008-kurrentdb-rename.md))

### Required consumer changes

1. Update NuGet references:
   ```diff
   - <PackageReference Include="Rig.TUnit.Databases.NoSql.EventStore" />
   + <PackageReference Include="Rig.TUnit.Databases.NoSql.KurrentDb" />
   ```
2. Update using directives:
   ```diff
   - using Rig.TUnit.Databases.NoSql.EventStore;
   + using Rig.TUnit.Databases.NoSql.KurrentDb;
   ```
3. Update fluent extensions:
   ```diff
   - rig.UseEventStore(…)
   + rig.UseKurrentDb(…)
   ```
4. Update connection strings — KurrentDB client uses `kurrentdb://` prefix:
   ```diff
   - esdb://localhost:2113?tls=false
   + kurrentdb://localhost:2113?tls=false
   ```

### Why the break
Upstream EventStore Ltd rebranded to Kurrent Inc. The .NET SDK shipped a clean-break
rename (`EventStore.Client` → `KurrentDB.Client`); a shim package would have created
permanent confusion in NuGet search and intellisense. See
[CHANGELOG.md § 0.3.0](../CHANGELOG.md) and
[ADR-008](adr/ADR-008-kurrentdb-rename.md) for the decision record.

## 0.3.0 → 0.4.0 (Feature 004 — Provider Consistency Remediation)

### Added
- Every leaf provider now ships the canonical quartet:
  - `{Provider}Fixture`
  - `{Provider}FixtureOptions` (with `public const string SectionName`)
  - `{Provider}RigBuilder`
  - `Use{Provider}` extension on `RigBuilder`
- `ProviderCompletenessTests` enforces this at CI time.
- `DatabasePerTestHelper` pattern established for SQL providers.

### Required consumer changes
None — the additions are purely additive. Existing `Use{Provider}` calls continue to
work.

### Optional consumer tightening
- Switch from `IConfiguration` reads to `IOptions<{Provider}FixtureOptions>`
  (see [ADR-003](adr/ADR-003-options-over-iconfiguration.md)). Default values
  remain identical.
- Replace per-test `{Provider}Fixture` construction with `SharedXxxFixture.GetAsync()`
  + a per-test helper (e.g., `CollectionPerTestHelper` for Mongo).

## 0.4.0 → 0.5.0 (Feature 005 — Legacy Coverage & Docs Parity, unreleased)

### Added
- `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` — per-test physical
  database isolation for Postgres tests.
- Full coverage gate (line ≥ 0.90 / branch ≥ 0.85) enforced by CI.
- `NoSkipMarkersTests` + `SharedFixtureGuardTests` regression guards.
- `benchmarks/baseline-005.json` with 20% regression budget.
- Root governance: `LICENSE`, `CONTRIBUTING.md` (full), `SECURITY.md`,
  `CHANGELOG.md`.
- `docs/templates/PROVIDER_README_TEMPLATE.md` + `docs/QUALITY-BAR.md`.
- 8 ADRs documenting the load-bearing architectural decisions.

### Required consumer changes
None — 005 is about hardening + documentation, not public API changes.

### Recommended consumer adoption
- Migrate Postgres tests from shared-database to
  `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` to inherit the flake fix.
- Pick up the new coverage threshold — consumer CI should mirror
  `line ≥ 0.90 / branch ≥ 0.85`.

## Full version compatibility matrix

| Version | .NET | TUnit | Testcontainers | Breaking? |
|---|---|---|---|---|
| 0.1.0 | 10.0 | 1.34.x | 4.x | n/a |
| 0.2.0 | 10.0 | 1.34.x | 4.x | No (additive) |
| 0.3.0 | 10.0 | 1.34.x | 4.11.0 | **Yes** — EventStore → KurrentDb |
| 0.4.0 | 10.0 | 1.34.5 | 4.11.0 | No (additive) |
| 0.5.0 (unreleased, 005) | 10.0 | 1.34.5 | 4.11.0 | No (additive) |
