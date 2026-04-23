# Review Report: 006 Coverage & Quality Uplift

**Date**: 2026-04-23 | **Mode**: Generic | **Reviewer**: /dotnet-ai-kit:review

## Scope

96 files changed, +8,845 / -55 against `master`.

- ~85 test files added (unit + integration)
- 2 production source edits (minimal): `Rig.TUnit.Caching.Memory.csproj` (`InternalsVisibleTo`), `ServiceBusFixture.cs` (`WithConfig` chain)
- 1 CI workflow update (`.github/workflows/ci.yml` — integration matrix expanded, coverage gate hardened)
- README rewrite (~+220 lines), spec/plan/tasks docs, planning notes, baseline-006.json, mlc_config.json

CodeRabbit CLI not detected — standards review only.

---

## Standards Review

### Check 1 — Naming Conventions
**Result**: PASS with 1 LOW finding.

- All test methods follow `{Method}_{Scenario}_{ExpectedResult}` (e.g., `UseMemoryCache_NullRig_ThrowsArgumentNullException`).
- All test classes are `sealed` and use file-scoped namespaces.
- **[LOW]** [tests/Rig.TUnit.Microservices.Outbox.Tests.Unit/OutboxAssertTests.cs:6](tests/Rig.TUnit.Microservices.Outbox.Tests.Unit/OutboxAssertTests.cs:6) — file is named `OutboxAssertTests.cs` but the class is `OutboxAssertContainsTests`. Either rename the file to match the class, or rename the class to `OutboxAssertTests`.

### Check 2 — Architecture Boundary Violations
**Result**: PASS.

- Production source changes are scoped and additive — no layer-crossing.
- `InternalsVisibleTo` added to a single test project (typical, justified).
- `ServiceBusFixture.WithConfig(...)` keeps concerns inside the fixture layer.

### Check 3 — Localization
**Result**: N/A. Project does not use localization patterns.

### Check 4 — Error Handling
**Result**: PASS. No empty catches, no `async void`, `CancellationToken` is propagated through integration tests.

### Check 5 — Testing
**Result**: PASS with 2 MEDIUM findings.

- **[MEDIUM]** [tests/Rig.TUnit.Ci.Tests.Unit/CiTests.cs:37-57](tests/Rig.TUnit.Ci.Tests.Unit/CiTests.cs:37) — `Path.GetTempFileName()` then `File.Delete(path)` at the end. If any assertion before line 57 throws, the temp file leaks. Wrap in `try/finally`, or use a per-test temp directory you delete in `IAsyncDisposable`. Same pattern likely exists in `TrxEnricher` tests further down.
- **[MEDIUM]** [tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/ServiceBusListenerTests.cs:36-39](tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/ServiceBusListenerTests.cs:36) — busy-poll loops with `await Task.Delay(200, ct)` (also lines 62-66, 90-94). For real-broker integration tests this is a defensible pattern, but the testing rule explicitly forbids `Task.Delay` in tests. Recommend extracting a single `WaitForAsync(predicate, timeout)` helper so the polling pattern lives in one place, is reusable, and the rule-violation is explicit and isolated.

  > Note: only integration tests do this. Unit-test scan showed zero `Task.Delay` / `Thread.Sleep` calls. Good.

### Check 6 — Security
**Result**: PASS. No hardcoded secrets, no string-concatenated SQL in changed files. `service-bus-config.json` is emulator config, not credentials.

### Check 7 — Event Structure
**Result**: N/A (generic mode, no new domain events introduced).

### Check 8 — Performance
**Result**: PASS.

### Check 9 — Brief Compliance
**Result**: N/A (single-repo project).

---

## CI Workflow — additional findings

- **[LOW]** [.github/workflows/ci.yml:358-363](.github/workflows/ci.yml:358) — comments on the coverage-threshold step are stale and contradict behaviour:
  - Lines 359-362 still say *"reverted to warn-only while per-provider test backfill is in flight"* — but the script below `sys.exit(1)`s on offenders, so it IS blocking.
  - Line 363 says *"Disabled 2026-04-20; re-enabled by feat/006 T090"* — this branch IS feat/006 T090, so the comment is now self-referential and misleading. Remove both stale comments (keep a single one-liner: `# Hard gate: line ≥ 0.90 / branch ≥ 0.70 (excludes integration-only providers below)`).

- **[INFO]** Integration matrix expansion at [ci.yml:294](.github/workflows/ci.yml:294) matches T001 (Core, Ci, Grpc, Http, WebAPI, Mediator). Verified.

---

## Working-tree hygiene

- **[LOW]** Two untracked files at repo root with mangled names (`C\357\200\272UserslibyaAppDataLocalTemprun-feat006.json`, `…run-regression.json`) — these are mojibake-encoded paths from accidental file creation on Windows. Delete them, and add `*.tmp.json` / a more targeted ignore to `.gitignore` so they never get staged.

---

## Summary

| Severity | Count |
|----------|------:|
| CRITICAL | 0 |
| HIGH     | 0 |
| MEDIUM   | 2 |
| LOW      | 4 |
| INFO     | 1 |

Auto-fixed: 0 (no `--auto-fix` requested).

**Verdict**: PASS WITH MINOR FIXES. Production source delta is tiny and safe; the bulk of the change is well-structured test code that follows existing conventions. The only quality items worth touching before merge are:
1. Rename `OutboxAssertContainsTests` to match its filename (or vice-versa).
2. Wrap the temp-file lifecycle in `CiTests` with `try/finally`.
3. Clean the stale comments on the CI coverage gate so future readers don't think the gate is disabled.
4. Delete the two mangled untracked files at repo root.

Optional follow-up: extract a `WaitForAsync` helper to centralise the integration-test polling pattern.

**Next**: `/dotnet-ai-kit:verify` (run build/test/format pipeline)
