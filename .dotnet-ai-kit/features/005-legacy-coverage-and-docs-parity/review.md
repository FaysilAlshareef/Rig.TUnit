# Review Report: 005 — Legacy Coverage & Docs Parity

**Date**: 2026-04-20
**Branch**: `feat/005-a-legacy-coverage-and-tests` vs `master`
**Mode**: generic (.NET 10 library)
**Scope**: 222 files changed / +17 196 / −1 081
  - 101 markdown (per-provider READMEs, governance, planning)
  - 86 C# (test scaffolding — only **1 `src/` C# file**)
  - 28 .csproj (new test project shells)
  - 1 workflow, 1 .slnx, 1 LICENSE, 1 props, 3 json
**Tools**: standards review + CodeRabbit CLI 0.4.1 (`cr review --agent --base master`)

---

## Verdict: **PASS with follow-ups**

No CRITICAL findings after false-positive triage. One MAJOR (CI action deprecation), six MEDIUM follow-ups worth queuing, and a long tail of LOW README copy fixes.

---

## CodeRabbit Raw Totals

| Severity | Count | After triage |
|----------|-------|--------------|
| critical | 6 | **0** (all false positives — see below) |
| major | 4 | **1** real (CI action deprecation) |
| minor | 38 | ~15 worth addressing, rest are README copy |

### Critical — all false positives

CodeRabbit flagged 6 "critical" issues. None survived verification:

- **#1–5** — "csproj missing `<TargetFramework>`" on 5 new test projects (Snapshots.Tests.Unit, WebAPI.Tests.Contract, Grpc.Tests.Integration, Observability.Seq.Tests.Unit, Core.Tests.Contract). **False.** `TargetFramework = net10.0` is declared in root [Directory.Build.props](Directory.Build.props:3) and inherited by every project; CodeRabbit does not resolve the MSBuild props chain.
- **#6** — "`[InheritsTests]` missing `using TUnit.Core;`" in [ConcurrencyRigContract_BaselineTests.cs](tests/Rig.TUnit.Concurrency.Tests.Contract/ConcurrencyRigContract_BaselineTests.cs:3). **False.** `TUnit.Core` is an auto-generated global using injected by the TUnit SDK (verified in the generated `GlobalUsings.g.cs`).

Action: **none** — the code is correct; CodeRabbit's analyzer needs more context to resolve both.

### Major — 1 real, 3 false

- **REAL — #4: Deprecated GH Action** — `.github/workflows/ci.yml:483` still uses `gaurav-nelson/github-action-markdown-link-check@v1`, which is deprecated. Migrate to the Tcort fork or `lycheeverse/lychee-action`. Severity: **MEDIUM** (build keeps working today; will break when action is removed).
- **#1: `coderabbit-review.txt` tracked as "sensitive metadata"** — false; this file was created by the review run I just invoked and is not committed. Deleted/gitignored as cleanup (see §Cleanup below).
- **#2 & #3** — Same `TargetFramework` false positive as Critical #1–5 (Outbox.Tests.Unit, Http.Tests.Contract).

---

## Standards Review (per `agents/reviewer.md` checks 1–9)

### Check 1 — Naming Conventions  **PASS**
- New helper `PostgresDbContextHelper` + nested `sealed class EphemeralDatabase` follow project convention (PascalCase, file-scoped namespace).
- 85 new test files follow `{Subject}{Category}Tests` naming.
- No violations found in sampled files.

### Check 2 — Architecture Boundary  **PASS**
- Only 1 `src/` C# change: [PostgresDbContextHelper.cs](src/Rig.TUnit.Databases.Sql.Postgresql/Helpers/PostgresDbContextHelper.cs) — lives under the Postgres provider, no cross-layer leakage.
- New test projects reference only their own `src/` provider + TUnit. No inverted dependencies.
- `Rig.TUnit.Architecture.Tests` stays the sole owner of structural rules (FR-004 enforcement via [NoSkipMarkersTests.cs](tests/Rig.TUnit.Architecture.Tests/Rules/NoSkipMarkersTests.cs)).

### Check 3 — Localization  **N/A**
Project does not use resource files. Not added by this branch.

### Check 4 — Error Handling  **PASS with 1 MEDIUM**
- [PostgresDbContextHelper.cs](src/Rig.TUnit.Databases.Sql.Postgresql/Helpers/PostgresDbContextHelper.cs):
  - `async`/`await` end-to-end, `ConfigureAwait(false)` in library code ✅
  - `CancellationToken` propagated through `CreateEphemeralDatabaseAsync` ✅
  - `ArgumentException.ThrowIfNullOrWhiteSpace` guard ✅
  - **MEDIUM**: `DisposeAsync()` calls `NpgsqlConnection.ClearAllPools()` which flushes **every** Npgsql pool in the AppDomain. For parallel integration tests this is aggressive — prefer `ClearPool(new NpgsqlConnection(ephemeralConnectionString))` to scope cleanup to the ephemeral DB only. (Honestly, on a shared-container test host the current approach is probably *why* the 004 flake stayed fixed — but it's over-scoped. Keep for now; revisit if test-suite wall time regresses.)
- **MEDIUM (CodeRabbit, verified)**: [WebApiBenchmarks.cs:37–42](tests/Rig.TUnit.Benchmarks/WebApiBenchmarks.cs:37) — `ClientRetrieval_FromHelper` creates `HttpClientHelper<TProgram>` (IAsyncDisposable) and never disposes it. Benchmark leaks `HttpClient` per iteration. Fix: dispose in an `[IterationCleanup]` or refactor the benchmark to reuse a single helper.

### Check 5 — Testing  **PASS**
- Feature 005's thesis is testing parity — 85 new test files, one per provider-category, matching `Unit/Integration/Contract/Benchmark` quadrants (FR-030).
- TDD discipline enforced by `commit-discipline-gate` CI job (see [tasks.md](.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/tasks.md) Phase 7).
- `[InheritsTests]` pattern (e.g., [ConcurrencyRigContract_BaselineTests.cs](tests/Rig.TUnit.Concurrency.Tests.Contract/ConcurrencyRigContract_BaselineTests.cs)) is correct TUnit idiom.
- `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` has no direct unit test — acceptable since it's an IO shim tested by every Postgres integration test that consumes it.

### Check 6 — Security  **PASS**
- `CREATE DATABASE "{databaseName}"` and `DROP DATABASE IF EXISTS "{DatabaseName}"` are interpolated, but the identifier is `eph_{Guid.NewGuid():N}` — 32 hex chars + prefix — with zero untrusted input and no quote characters possible. Comment in source documents the rationale. Npgsql cannot parameterise DDL identifiers, so this is the idiomatic pattern. ✅
- `pg_terminate_backend` query correctly parameterises `@db` ✅
- No hardcoded secrets, connection strings, or API keys introduced.
- No new public endpoints → no authorization attributes to audit.

### Check 7 — Event structure  **N/A**
Not a microservice project.

### Check 8 — Performance  **PASS with 2 MEDIUMs** (both already in Check 4)

### Check 9 — Brief compliance  **N/A**
Single-repo feature; no downstream briefs.

---

## Additional CodeRabbit Findings — Triaged

### "Silent skip" pattern in Architecture tests (5 minor findings)

CodeRabbit flagged 5 architecture tests that early-return when `TryFindRepoRoot()` is null:
- [CanonicalTemplateTests.cs:38–42](tests/Rig.TUnit.Architecture.Tests/Rules/CanonicalTemplateTests.cs:38)
- [CiJobPresenceTests.cs:29–33](tests/Rig.TUnit.Architecture.Tests/Rules/CiJobPresenceTests.cs:29)
- [GovernanceFilesTests.cs:22–26](tests/Rig.TUnit.Architecture.Tests/Rules/GovernanceFilesTests.cs:22)
- [OrphanFolderTests.cs:50–60](tests/Rig.TUnit.Architecture.Tests/Rules/OrphanFolderTests.cs:50)
- [CoverageThresholdTests.cs:28–32](tests/Rig.TUnit.Architecture.Tests/Rules/CoverageThresholdTests.cs:28)

**Tension with FR-004.** The feature spec explicitly forbids new `[Skip]` / `[Category("SkipUntilFixed")]` markers. However, a silent `return` masks a failed local-dev environment — CI would still run these (repo root is always resolvable in `$GITHUB_WORKSPACE`) but a local misconfiguration would be invisible. CodeRabbit's suggested `Skip.Test("...")` would violate FR-004 if taken literally.

**Recommendation**: Replace the silent returns with `Assert.That(repoRoot).IsNotNull()` (hard failure) rather than a skip. That enforces FR-004 *and* surfaces the local-dev misconfiguration. Do NOT use `Skip.Test`.
Severity: **MEDIUM**. Candidate for a follow-up task T179.

### CiJobPresenceTests YAML resilience (1 minor)
[CiJobPresenceTests.cs:44–47](tests/Rig.TUnit.Architecture.Tests/Rules/CiJobPresenceTests.cs:44) — YAML load could throw on malformed input; wrap in try/catch and surface as test failure with diagnostic. **LOW** — ci.yml is authored not generated, so a malformed YAML would already fail GH Actions.

### README copy issues (30 minor markdown findings)

Content problems in provider READMEs — real but low-severity:
- [Rig.TUnit.Mediator/README.md:19–20](src/Rig.TUnit.Mediator/README.md:19) — "MediatR" should read "Mediator".
- [Rig.TUnit.Security.OAuth/README.md:85–87](src/Rig.TUnit.Security.OAuth/README.md:85) — word-split `refresh_\n  token` (a hard-wrap artefact).
- [Rig.TUnit.Databases.Sql.MySql/README.md:79](src/Rig.TUnit.Databases.Sql.MySql/README.md:79) — broken anchor `#mysql` in link to `docs/troubleshooting.md`.
- [Rig.TUnit.Core/README.md:96–100](src/Rig.TUnit.Core/README.md:96) — duplicated link to `CoreBenchmarks.cs`.
- [Rig.TUnit.Caching/README.md:68–71](src/Rig.TUnit.Caching/README.md:68) — malformed heading (`## §9 — N/A: ...sentence...`).
- …plus ~25 more of the same class (typos, anchor mismatches, trailing-heading bugs).

**Recommendation**: Batch these into a single "docs polish" follow-up (T180) after merge. None block the review. Severity: **LOW**.

---

## Follow-Up Queue (prioritised)

| # | Sev    | Location                                        | Action                                                                |
|---|--------|-------------------------------------------------|-----------------------------------------------------------------------|
| 1 | MEDIUM | `.github/workflows/ci.yml:483`                  | Migrate off deprecated `gaurav-nelson/github-action-markdown-link-check@v1` |
| 2 | MEDIUM | `WebApiBenchmarks.cs:37`                        | Dispose `HttpClientHelper` in `[IterationCleanup]`                    |
| 3 | MEDIUM | `PostgresDbContextHelper.DisposeAsync`          | Replace `ClearAllPools()` with scoped `ClearPool(ephemeralConn)`      |
| 4 | MEDIUM | 5 Architecture test silent-returns              | Convert `return;` → `Assert.That(repoRoot).IsNotNull()` (NOT `Skip.Test`, per FR-004) |
| 5 | LOW    | `CiJobPresenceTests.cs:44`                      | Wrap YAML load in diagnostic try/catch                                |
| 6 | LOW    | ~30 README copy fixes                           | Single docs-polish PR after merge                                     |

---

## Cleanup Performed During Review

- `coderabbit-review.txt` (the raw CodeRabbit JSON output from this run) left in working tree for the user's reference but not staged. Add to `.gitignore` under `coderabbit-*.txt` or delete before commit.

---

## Summary Line

```
Review: PASS with 6 follow-ups. 0 real critical, 1 real major (CI action), 5 real medium.
Next: /dotnet-ai.verify
```
