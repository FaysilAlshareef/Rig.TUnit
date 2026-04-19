# Data Model: 005-legacy-coverage-and-docs-parity

**Generated**: 2026-04-19
**Scope**: This feature ships no new domain entities or database schemas — it's a test-library remediation. The "entities" here are operational artefacts (test-project shapes, commit message formats, CI gate inputs/outputs, README section structure, skip-marker lifecycle). Each entity has a schema, a producer, a consumer, and a validator.

---

## Entity 1 — Test-project tuple

**Definition.** A canonical provider's test surface is the tuple `{ Unit, Integration, Contract, Benchmark }`. `TestCompletenessTests` enforces presence across all 63 src projects (with edge-case `N/A` markers).

### Schema

For `src/Rig.TUnit.{Family}.{Provider}/`, the required tuple is:

| Slot | Path | Required content | N/A condition |
|---|---|---|---|
| Unit | `tests/Rig.TUnit.{Family}.{Provider}.Tests.Unit/` | ≥ 1 `[Test]` on Options validation + RigBuilder wiring | Abstract base / analyser / meta |
| Integration | `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/` | ≥ 1 `[Test]` exercising the real fixture end-to-end | Pure-abstraction packages |
| Contract | `tests/Rig.TUnit.{Family}.Tests.Contract/` (shared) | The provider inherits `{Family}RigContract` | Cross-cutting packages without a family base |
| Benchmark | `tests/Rig.TUnit.Benchmarks/{Provider}Benchmarks.cs` | ≥ 1 `[Benchmark]` + `[MemoryDiagnoser]` | Analyser / meta packages |

### Producer

Phase 3 tasks T020–T042 create missing slots.

### Consumer

`TestCompletenessTests.cs` (lines 22-53 hold the skip list today; emptied by Phase 3 end per FR-036).

### Validator

```csharp
// Pseudocode — actual rule lives in tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs
foreach (var srcProject in SolutionLoader.AllSrcProjects())
{
    if (IsExemptMetaOrAnalyzer(srcProject)) continue;

    Assert.True(Directory.Exists($"tests/{srcProject.Name}.Tests.Unit"));
    Assert.True(Directory.Exists($"tests/{srcProject.Name}.Tests.Integration") ||
                HasExplicitNaMarker(srcProject, "Integration"));
    Assert.True(ShipsContractTest(srcProject));
    Assert.True(BenchmarksProject.HasBenchmarkFor(srcProject.Name) ||
                HasExplicitNaMarker(srcProject, "Benchmark"));
}
```

### Lifecycle

- **Before Phase 3**: 23 projects fail this check (currently skipped via `SkipUntilFixed`).
- **During Phase 3**: each RED commit adds the missing tests; each GREEN commit implements the production change + removes the skip entry for that provider.
- **After Phase 3**: skip list is empty; the rule enforces on every PR.

---

## Entity 2 — Provider-shape quartet

**Definition.** Every `src/Rig.TUnit.{Family}.{Provider}/` ships four mandatory production classes: `{Provider}Fixture`, `{Provider}FixtureOptions`, `{Provider}RigBuilder`, `{Provider}RigBuilderExtensions.Use{Provider}`.

### Schema

```
Fixtures/{Provider}Fixture.cs
  public sealed class {Provider}Fixture : {Family}FixtureBase { … }

Options/{Provider}FixtureOptions.cs
  public sealed class {Provider}FixtureOptions
  {
      public const string SectionName = "{Provider}";
      [Required] public required string {Prop} { get; init; }
      …
  }

Builder/{Provider}RigBuilder.cs
  public sealed class {Provider}RigBuilder : {Family}RigBuilder<{Provider}RigBuilder> { … }

Builder/{Provider}RigBuilderExtensions.cs
  public static class {Provider}RigBuilderExtensions
  {
      public static RigBuilder Use{Provider}(
          this RigBuilder rig,
          Action<{Provider}RigBuilder>? configure = null) { … }
  }
```

Family-specific helpers (FR-042):

| Family | Additional required |
|---|---|
| Messaging | `Helpers/{Provider}Listener.cs : ListenerBase`, `Helpers/{Provider}EventSender.cs : EventSenderBase` |
| Storage | `Helpers/{Provider}SasBuilder.cs` (AzureBlob, S3, MinIO) or `Helpers/PathSandboxHelper.cs` (FileSystem) |
| Observability.Metrics | `Helpers/TagCardinalityGuard.cs` |

### Producer

Phase 4 tasks fill the quartet for ~20 pre-004 providers that ship fixture-only or missing Options/Builder.

### Consumer

`ProviderCompletenessTests.cs`.

### Validator

```csharp
// Actual rule: tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs
foreach (var provider in ProviderRegistry.AllProviderAssemblies())
{
    Assert.NotNull(provider.GetType($"{provider.Name}.Fixtures.{provider.ProviderName}Fixture"));
    Assert.NotNull(provider.GetType($"{provider.Name}.Options.{provider.ProviderName}FixtureOptions"));
    Assert.NotNull(provider.GetType($"{provider.Name}.Builder.{provider.ProviderName}RigBuilder"));
    Assert.True(provider.HasExtensionMethod($"Use{provider.ProviderName}", typeof(RigBuilder)));

    // Family-specific
    if (provider.Family == Messaging) Assert.NotNull(provider.GetType("…Listener"));
    // ... etc
}
```

### Lifecycle

- **Before Phase 4**: ~20 providers fail (tracked via `SkipUntilFixed` in `ProviderCompletenessTests`).
- **During Phase 4**: each RED commit adds a failing assertion for the missing quartet member; each GREEN commit fills the class and removes the skip.
- **After Phase 4**: skip list empty; rule enforces uniformly.

---

## Entity 3 — Commit-message envelope

**Definition.** Every commit on `feat/005-*` follows a structured subject line. The `commit-discipline-gate` CI job parses and validates pairs.

### Schema

```
<type>(005): T<nnn> — <phase-marker> <description>

   type         : test | feat | docs | ci | chore | refactor
   T<nnn>       : task ID from tasks.md
   phase-marker : RED | GREEN  (required for test/feat; optional for docs/ci/chore)
   description  : imperative, ≤ 72 chars
```

Examples:

- `test(005): T020 — RED for Rig.TUnit.Core Integration + Contract`
- `feat(005): T020 — GREEN implement Rig.TUnit.Core Integration + Contract`
- `docs(005): T121 — GREEN root LICENSE + CONTRIBUTING + SECURITY`
- `ci(005): T007 — GREEN HTML report upload`
- `chore(005): bump Markdig pin for README parser` (no T ID required for non-task chores)

### Constraints

1. Every commit with `feat(005): T<nnn> — GREEN` MUST be immediately preceded (same branch, no intervening commit) by a matching `test(005): T<nnn> — RED` with the same `T<nnn>`.
2. No commit may carry the subject `test(005): T<nnn> — RED` AND pass all touched-project tests (FR-003: `red-commit-verification`).
3. Exemption: SHA `2b149b2` (Feature 004 Phase 3.0 retroactive, per FR-002 / [004 FR-034](../004-provider-consistency-remediation/spec.md)). No other SHA may be added to the exemption list without a spec amendment.

### Producer

Every task in tasks.md.

### Consumer

`.github/workflows/ci.yml` → `commit-discipline-gate` job (Phase 7 T168/T169) + `red-commit-verification` step (Phase 7 T170/T171).

### Validator

`.github/workflows/ci.yml` job (shell script, see [research.md R10](research.md)).

---

## Entity 4 — Coverage gate input

**Definition.** Per-package coverage measurements consumed by the `coverage-summary` CI job.

### Schema

Per-package cobertura XML under `<proj>/bin/Release/net10.0/TestResults/coverage.cobertura.xml`:

```xml
<coverage line-rate="0.94" branch-rate="0.88" …>
  <packages>
    <package name="Rig.TUnit.Caching.Memory" line-rate="0.92" branch-rate="0.86">…</package>
  </packages>
</coverage>
```

Merged output: `./coverage-report/Cobertura.xml` + `./coverage-report/SummaryGithub.md`.

### Producer

Every integration matrix job + `build-unit-arch` (Phase 2 T011 adds the `--coverage` flag).

### Consumer

1. `coverage-summary` CI job (Phase 2 T013).
2. Threshold check step (Phase 2 T015): fails the job if any `<package>` has `line-rate < 0.90` or `branch-rate < 0.85`.
3. `benchmarks/coverage-baseline-005.json` (Phase 2 T016): snapshot for Phase 3 before/after comparison.

### Validator

```bash
# coverage-summary job, threshold step
python3 <<EOF
import xml.etree.ElementTree as ET
tree = ET.parse("./coverage-report/Cobertura.xml")
violators = []
for pkg in tree.iter("package"):
    name, lr, br = pkg.get("name"), float(pkg.get("line-rate")), float(pkg.get("branch-rate"))
    if lr < 0.90 or br < 0.85:
        violators.append((name, lr, br))
if violators:
    for v in violators:
        print(f"::error::Package {v[0]} below threshold: line={v[1]:.2%} branch={v[2]:.2%}")
    exit(1)
EOF
```

### Lifecycle

- **Phase 2 non-blocking**: threshold step runs but doesn't fail the build; generates `coverage-baseline-005.json`.
- **Phase 3 per-provider**: each provider closes its gap below threshold with FR-038 coverage-lifting tests.
- **Phase 3 close**: flip threshold step to blocking. From that commit forward, every PR that drops coverage below 90/85 fails CI.

---

## Entity 5 — Benchmark baseline

**Definition.** Per-provider BenchmarkDotNet JSON snapshot used by the `benchmark-regression` CI job to detect > 20 % regression on PRs.

### Schema

`benchmarks/baseline-005.json`:

```json
{
  "meta": {
    "generated_at": "2026-04-25T00:00:00Z",
    "dotnet_sdk": "10.0.100",
    "benchmarkdotnet_version": "0.14.0",
    "commit_sha": "<close-out-commit-of-phase-3>"
  },
  "providers": {
    "Rig.TUnit.Caching.Memory": {
      "InitializeAsync_Cold": { "mean_ns": 1234.5, "alloc_bytes": 456 },
      "SetAsync_Warm":        { "mean_ns": 89.1,   "alloc_bytes": 24  }
    },
    "Rig.TUnit.Databases.Sql.SqlServer": { … }
  }
}
```

### Producer

Phase 3 close-out commit. Generated by `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --exporters json --artifacts ./benchmark-results` + a one-off merge script.

### Consumer

`benchmark-regression` CI job (Phase 7 T166/T167):

```bash
# Parse new run; compare per-provider per-method mean_ns; fail if > 20 % regression
new=$(dotnet run … --exporters json --artifacts ./out)
python3 compare-baseline.py benchmarks/baseline-005.json ./out --threshold 1.20
```

### Validator

`tests/Rig.TUnit.Benchmarks/BaselinePresenceTests.cs` (add in Phase 3 close-out): asserts `baseline-005.json` exists and has an entry for every non-N/A provider.

### Lifecycle

- **Before Phase 3 close**: no baseline; PRs skip regression check.
- **At Phase 3 close**: baseline written, gate goes live.
- **After Phase 3**: every PR runs benchmarks, compares, and fails if any metric regresses > 20 % vs baseline.

---

## Entity 6 — Skip-marker registry

**Definition.** The `[Category("SkipUntilFixed")]` markers inherited from Feature 004, across 4 architecture rule files. 005 retires every one.

### Schema

Rule file | Retired by phase | Phase task
|---|---|---|
`TestCompletenessTests.cs` (lines 22-53) | Phase 3 | T020–T042 (one entry removed per provider GREEN commit)
`ProviderCompletenessTests.cs` | Phase 4 | T04x family batches (one entry removed per provider GREEN commit)
`TestFileOrganizationTests.cs` | Phase 5 | T05x per-project (one entry removed per project GREEN commit)
`ReadmeCompletenessTests.cs` | Phase 6c (per-family trim) + 6d T157/T158 (residual sweep) | revised per analyze #2/#3 — each Phase 6c family GREEN trims its family's entries; T158 cleans up any residuals

### Producer

The 4 files above — frozen state as inherited from 004.

### Consumer

Every architecture-rule test run. `grep -rn "SkipUntilFixed" tests/` is the audit command.

### Validator

Final audit step in the Feature 005 merge PR:

```bash
if [ $(grep -rn 'SkipUntilFixed' tests/ | wc -l) -gt 0 ]; then
    echo "::error::SkipUntilFixed markers remain — Feature 005 exit gate failed (SC-012)"
    grep -rn 'SkipUntilFixed' tests/
    exit 1
fi
```

### Lifecycle constraint (FR-004)

No NEW `SkipUntilFixed` / `Skip` / permanent `NotInParallel` marker may be introduced in 005. A PR that introduces one fails the audit step above (which also runs on every PR, not just the merge PR). Temporary `[NotInParallel]` on a specific test is acceptable ONLY if the same PR commits a Phase-3 conversion ticket in the shared-fixture audit document.

---

## Entity 7 — 14-section canonical README

**Definition.** The structural schema that every `src/Rig.TUnit.{X}/README.md` satisfies after Phase 6c. Validated by the `Markdig`-parsed `ReadmeCompletenessTests` after Phase 6d.

### Schema (section presence + order)

```
(top of file — badges: NuGet | downloads | CI | coverage | licence)
# {Package Name}
## Purpose & value
## When NOT to use
## Install
## Quick start
## Configuration
## API surface
## Fluent wiring
## Provider quirks
## Troubleshooting
## Testing contracts
## Performance
## Dependencies & related packages
## Spec, versioning, contributing
```

Section 6 has a reflected-options constraint:

```
## Configuration

| Property | Type | Default | Required? | Validation | Purpose |
|---|---|---|---|---|---|
| ConnectionString | string | — | Yes | [Required] | Container connection URL |
| …
```

The Options-table rows MUST match `{Provider}FixtureOptions.cs` properties exactly (name, default value, `[Required]` flag). `ReadmeCompletenessTests` asserts this via reflection.

Section 12 has a benchmark-link constraint:

```
## Performance

Benchmarks live in [tests/Rig.TUnit.Benchmarks/MemoryCacheBenchmarks.cs](…).
```

The link MUST resolve to an existing class in the Benchmarks project.

### Variant for base / meta packages (§3.2)

Sections 9, 10, 12 may be replaced with `## §N — N/A: <one-line rationale>`.

### Producer

Phase 6c tasks T137–T156 (10 per-family batches, one PR each).

### Consumer

`ReadmeCompletenessTests.cs` (Phase 6a T123b/T123c rewrite using `Markdig`; moved forward from Phase 6d per analyze #2/#3).

### Validator

`tests/Rig.TUnit.Architecture.Tests/Rules/ReadmeCompletenessTests.cs` walks `src/Rig.TUnit.*/README.md`, parses with `Markdig`, asserts section presence + reflective Options table + benchmark link resolution.

### Lifecycle

- **Before Phase 6**: all 63 READMEs fail the new structural gate (only the legacy `> 100 chars` gate passes, and only for the 51 that exist).
- **Phase 6a/6b**: template + supporting docs; no per-leaf changes yet.
- **Phase 6c**: family batches rewrite READMEs against the template (they still pass the old gate because content is there; the new gate doesn't exist yet).
- **Phase 6a (moved forward)**: T123b adds Markdig pin; T123c rewrites `ReadmeCompletenessTests` to enforce structural gate (RED against placeholder READMEs); T123d expands skip list for rollout.
- **Phase 6c**: each family GREEN commit trims its family's skip entries.
- **Phase 6d**: T157/T158 residually ensures the skip list is empty. Every README passes.

---

## Entity 8 — Orphan folder audit

**Definition.** Three directories present on disk but not referenced in `Rig.TUnit.slnx`; each contains only `bin/obj/`. Deleted in Phase 1.

### Schema

| Path | Reason | Deleted in |
|---|---|---|
| `src/Rig.TUnit.ServiceBus/` | Pre-rename (→ `Rig.TUnit.Messaging.ServiceBus`) | T002 GREEN |
| `tests/Rig.TUnit.ServiceBus.Tests.Integration/` | Same | T002 GREEN |
| `tests/Rig.TUnit.SqlServer.Tests.Integration/` | Pre-rename (→ `Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration`) | T002 GREEN |

### Producer

Feature 003 pre-rename artefacts left in the tree.

### Consumer

`tests/Rig.TUnit.Architecture.Tests/Rules/OrphanFolderTests.cs` (added in T001 RED).

### Validator

```csharp
// T001 RED
[Test] public void OrphanFolders_MustNotExist()
{
    Assert.False(Directory.Exists("src/Rig.TUnit.ServiceBus"));
    Assert.False(Directory.Exists("tests/Rig.TUnit.ServiceBus.Tests.Integration"));
    Assert.False(Directory.Exists("tests/Rig.TUnit.SqlServer.Tests.Integration"));
}
```

### Lifecycle

- **T001 RED**: test added; fails because folders still exist.
- **T002 GREEN**: `git rm -r` all three; test passes; no future reintroduction possible because the rule runs on every PR.

---

## References

- [plan.md](plan.md) — phase plan and task references
- [research.md](research.md) — evidence for each entity's producer / consumer mapping
- [spec.md](spec.md) — functional requirements that constrain each entity's validator
- [Documentation-Audit.md §3.1](../../../planning/post-004-remediation/Documentation-Audit.md) — Entity 7 authoritative section list
- [Test-Coverage-Gap-Matrix.md](../../../planning/post-004-remediation/Test-Coverage-Gap-Matrix.md) — Entity 1 initial skip list
- [CI-Artifact-And-Coverage-Proposal.md](../../../planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md) — Entity 4 coverage YAML
