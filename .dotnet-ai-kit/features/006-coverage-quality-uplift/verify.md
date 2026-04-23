# Verification Report: 006 Coverage & Quality Uplift

**Feature**: 006-coverage-quality-uplift | **Date**: 2026-04-23 | **Mode**: Generic
**Scope**: build + unit tests + format (integration tests skipped per request)

## Results

| Repo | Build | Unit Tests | Resources | Proto | K8s | Format | Overall |
|------|-------|------------|-----------|-------|-----|--------|---------|
| Rig.TUnit | PASS | PASS (1486/0) | SKIP | SKIP | SKIP | PASS* | **PASS** |

\* 2 pre-existing import-ordering errors remain on files not touched by this branch — see "Format" section.

## Details

### Build
- Command: `dotnet build Rig.TUnit.slnx --no-restore -c Release`
- Result: **PASS** (0 errors, 0 warnings)
- Time: 1m 49s

### Unit + Architecture Tests
- 60 projects executed (filter: exclude `Integration|Benchmarks|Tests.Contract`, matches `build-unit-arch` job in [ci.yml:30](.github/workflows/ci.yml:30))
- Result: **PASS** — 60/60 projects passed
- Total tests: **1,486 passed / 0 failed / 0 skipped**
- Highest project: `Rig.TUnit.Caching.Fusion.Tests.Unit` (41), `Rig.TUnit.Storage.MinIO.Tests.Unit` (34), `Rig.TUnit.Architecture.Tests` (33)

### Resources / Proto / K8s
- **SKIP** — none present in scope.

### Format (`dotnet format --verify-no-changes`)
- **14 line-ending fixes applied** to branch-added files (`MockOAuthServerTests.cs`, `RedisFixtureTests.cs`, `StampedeTesterTests.cs`, `ChangeFeedCaptureTests.cs`, `HttpRequestBuilderExtendedTests.cs`, `ContractAssertTests.cs`, `OutboxAssertDeadLetterTests.cs`, `OutboxEntryAssertionExtendedTests.cs`, `OutboxRelaySimulatorTests.cs`, `OutboxSchemaTests.cs`, `SagaHarnessExtendedTests.cs`, `AntiPatternDetectorTests.cs`, `LogAssertTests.cs`, `JwtBuilderTests.cs`) — all normalised LF → CRLF.
- `PolicyAssertTests.cs` was initially included in the LF→CRLF batch, but the PowerShell `-replace` step accidentally case-folded several `N` characters (`AspNetCore`→`AspnetCore`, `NullPrincipal`→`nullPrincipal`). The file was reverted via `git checkout HEAD --` (which restored CRLF on checkout via `core.autocrlf=true`) and re-tested clean.
- **1 whitespace fix applied** in `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/ServiceBusListenerTests.cs:13-18` — extra alignment spaces in `private const string` declarations.
- **2 errors remain (pre-existing, NOT this branch)**:
  - [src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs:1](src/Rig.TUnit.Storage.FileSystem/Fixtures/FileSystemFixture.cs:1) — `IMPORTS: Fix imports ordering`
  - [tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs:1](tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs:1) — `IMPORTS: Fix imports ordering`
  - Confirmed via `git diff master...HEAD --name-only` — neither file is in the feat/006 changeset; they are inherited from master.
  - CI does not run `dotnet format --verify-no-changes`, so these do not block the pipeline. Track in a follow-up cleanup.

### Skipped per user request
- All `*.Tests.Integration` projects (10 integration matrices in CI: SQL, NoSQL, Caching, Messaging, Microservices, Security, Observability, Storage, Core).
- Re-run with the full integration matrix in CI before merge (see `Integration — *` jobs in [ci.yml](../../../.github/workflows/ci.yml)).

## Summary

```
Verification (build + unit + format) for 006-coverage-quality-uplift:
  Build:        PASS (0 errors, 0 warnings) — re-built after PolicyAssertTests revert
  Unit tests:   PASS (60 projects, 1486 tests, 0 failed) — re-run, identical result
  Format:       PASS for branch-touched files (15 fixes applied)
  Format:       2 pre-existing errors remain (not in branch scope)

Overall: PASS — feature is ready for the integration-test sweep in CI, then PR.
```

**Next**: push branch, watch the integration matrices in CI; if green → `/dotnet-ai-kit:pr`.
