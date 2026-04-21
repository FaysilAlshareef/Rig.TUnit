# Benchmark Remediation Plan — Feature 006

**Audit date**: 2026-04-21  
**Scope**: Fix the three confirmed infrastructure defects found during the 2026-04-21 coverage scan;
make benchmark numbers meaningful and regression detection functional.

---

## Defect 1 — `InProcessEmitBenchmarkConfig` targets .NET 8, not .NET 10

### Evidence

`tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs`:

```csharp
.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)  // line ~18
```

The solution targets `net10.0` in every `.csproj`.  All benchmarks that use this config run
against the .NET 8 runtime, so the numbers are wrong for 22 of 55 benchmark files.

### Fix (T040)

Change the runtime to `CoreRuntime.Core100`:

```csharp
// Before
.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core80)

// After
.WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core100)
```

`CoreRuntime.Core100` was added in BenchmarkDotNet 0.14.0 (released January 2025) and is available
in the version already in the solution.  Verify the exact constant name against the installed
BenchmarkDotNet package before committing.

**File**: `tests/Rig.TUnit.Benchmarks/InProcessEmitBenchmarkConfig.cs`  
**Effort**: 30 min (change + verify build)  
**Risk**: Low — one-line fix; existing benchmarks re-run under the correct runtime

---

## Defect 2 — Baseline file is empty; regression detection never fires

### Evidence

`benchmarks/baseline-005.json`:

```json
{ "benchmarks": {} }
```

The regression script in `ci.yml` compares current run output against this baseline.  Because the
baseline has no entries, no regression can ever be detected and the CI step is silently a no-op.

### Fix (two-step)

#### Step 1 — T041: Run benchmarks locally, capture baseline

Run the full benchmark suite in `Release` configuration to capture a real baseline:

```bash
dotnet run --project tests/Rig.TUnit.Benchmarks \
  -c Release \
  -- --filter "*" \
     --exporters json \
     --artifacts benchmarks/baseline-tmp
```

Copy the generated `BenchmarkDotNet.Artifacts/results/*-report-full.json` content into
`benchmarks/baseline-005.json` (or rename to `baseline-006.json` to reflect Feature 006).  The
JSON schema accepted by the regression script is BenchmarkDotNet's native full-report format.

> Note: Run only after Defect 1 is fixed (T040) so the baseline reflects .NET 10 numbers.

#### Step 2 — T042: Update the CI regression script path

If the baseline file is renamed (`baseline-006.json`), update the `ci.yml` reference:

```yaml
# Before
--baseline benchmarks/baseline-005.json

# After
--baseline benchmarks/baseline-006.json
```

Also remove the `|| echo "::warning::..."` guard (see `CI-Pipeline-Gap-Audit.md` Gap 4, T050) so
the check can block merges once the baseline is populated.

**Files**: `benchmarks/baseline-005.json` (or new `baseline-006.json`), `.github/workflows/ci.yml`  
**Effort**: T041 ~2 h (run + review numbers); T042 < 15 min

---

## Defect 3 — No visualisation of benchmark results

### Current state

Benchmark results are uploaded as raw CI artefacts (ZIP download) but there is no dashboard,
trend chart, or badge.  Consumers of the library cannot see performance characteristics without
downloading and parsing JSON manually.

### Fix (T043 — recommended approach)

#### Option A — GitHub Pages HTML report (recommended)

After each `master` merge, generate a BenchmarkDotNet HTML report and publish to `gh-pages`:

```yaml
- name: Publish benchmark report
  if: github.ref == 'refs/heads/master'
  uses: peaceiris/actions-gh-pages@v4
  with:
    github_token: ${{ secrets.GITHUB_TOKEN }}
    publish_dir: ./benchmarks/html-report
```

The HTML report is self-contained; no external service required.

#### Option B — `benchmark-action/github-action-benchmark`

The `benchmark-action/github-action-benchmark@v1` action produces a trend chart hosted on GitHub
Pages.  It supports BenchmarkDotNet JSON natively.  Free, no third-party service.

Minimal config:

```yaml
- uses: benchmark-action/github-action-benchmark@v1
  with:
    tool: 'benchmarkdotnet'
    output-file-path: benchmarks/latest.json
    github-token: ${{ secrets.GITHUB_TOKEN }}
    auto-push: true
    alert-threshold: '120%'
    comment-on-alert: true
    fail-on-alert: true
```

This also replaces the hand-rolled regression script with a maintained action.

#### Option C — README badge only

Generate a single-number badge (e.g., mean latency of the hottest benchmark) via
`shields.io/endpoint` pointing to a JSON file committed to `gh-pages`.  Lightweight but
shows only one metric.

**Recommended**: Option B — replaces the custom regression script, provides a trend chart, and
handles alerts automatically.

**Effort**: T043 ~3–4 h (setup + first successful run)  
**Risk**: Requires `gh-pages` branch to be created and GitHub Pages to be enabled in repo settings.

---

## Delivery order

| Task | Description | Depends on | Effort |
|------|-------------|-----------|--------|
| T040 | Fix `.WithRuntime` to `Core100` | — | 30 min |
| T041 | Run benchmarks locally; capture baseline JSON | T040 | 2 h |
| T042 | Update baseline path + remove `\|\| echo` guard | T041 | 15 min |
| T043 | Add `github-action-benchmark` visualisation | T042 | 3–4 h |

---

## Benchmark files using `InProcessEmitBenchmarkConfig` (22 of 55)

To identify affected files:

```bash
grep -rl "InProcessEmitBenchmarkConfig" tests/Rig.TUnit.Benchmarks/
```

Files that do NOT use this config use `ManualConfig` or `DefaultConfig` and may already target the
correct runtime via the `[SimpleJob(RuntimeMoniker.Net100)]` attribute.  Verify both groups after
T040.
