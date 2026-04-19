# Quickstart: 005-legacy-coverage-and-docs-parity

**Generated**: 2026-04-19
**Audience**: the engineer picking up Feature 005 tasks (may be the same person, may not). Self-contained onboarding.

---

## 1. Prerequisites (one-time)

```bash
# .NET 10 SDK pinned via global.json
dotnet --version          # expect 10.0.100 (or later per rollForward)
dotnet test --list        # expect Microsoft.Testing.Platform (not VSTest)

# Docker for integration tests (some fixtures need containers)
docker info

# gh CLI for PR automation
gh --version

# Project state
cd C:\Users\libya\source\repos\Ecom-LTD\Rig.TUnit
git status                # clean working tree
git branch --show-current # should be master or a feat/005-* branch
```

---

## 2. Branch strategy recap

| Branch | Off | Merges to | Scope |
|---|---|---|---|
| `fix/005-phase-1-ci-stabilisation` | `master` | `master` (hotfix) | Phase 1 only |
| `feat/005-a-legacy-coverage-and-tests` | `master` (after Phase 1) | `master` | Phases 2, 3, 4, 5, 7 |
| `feat/005-b-docs-parity` | `master` (any time after Phase 1) | `master` | Phase 6 (6a→6b→6c→6d strict) |

Both 005-a and 005-b merge before Feature 005 is closed. File-level independent (tests vs docs).

---

## 3. The one golden rule

**RED commit MUST precede GREEN commit for every `src/`-touching task.** No exceptions except the retroactive `2b149b2` (Feature 004's one known violation).

```
test(005): TNNN — RED for {summary}
feat(005): TNNN — GREEN implement {summary}
```

The `commit-discipline-gate` CI job parses these subjects on PR. A missing RED → PR fails. A GREEN that passes all tests (secretly not red) → PR fails. **No retries. Red is red.**

---

## 4. Phase 1 kick-off recipe

```bash
git checkout master
git pull
git checkout -b fix/005-phase-1-ci-stabilisation

# T001 RED — add OrphanFolderTests
cat > tests/Rig.TUnit.Architecture.Tests/Rules/OrphanFolderTests.cs <<'EOF'
namespace Rig.TUnit.Architecture.Tests.Rules;

public sealed class OrphanFolderTests
{
    [Test] public void ServiceBus_SrcFolder_MustNotExist()
        => Assert.That(Directory.Exists("src/Rig.TUnit.ServiceBus")).IsFalse();

    [Test] public void ServiceBus_TestFolder_MustNotExist()
        => Assert.That(Directory.Exists("tests/Rig.TUnit.ServiceBus.Tests.Integration")).IsFalse();

    [Test] public void SqlServer_TestFolder_MustNotExist()
        => Assert.That(Directory.Exists("tests/Rig.TUnit.SqlServer.Tests.Integration")).IsFalse();
}
EOF

# Verify RED
dotnet test tests/Rig.TUnit.Architecture.Tests --filter "FullyQualifiedName~OrphanFolderTests"
# expect non-zero exit

git add tests/Rig.TUnit.Architecture.Tests/Rules/OrphanFolderTests.cs
git commit -m "test(005): T001 — RED for orphan folder deletion audit"

# T002 GREEN — delete folders
git rm -r src/Rig.TUnit.ServiceBus tests/Rig.TUnit.ServiceBus.Tests.Integration tests/Rig.TUnit.SqlServer.Tests.Integration
dotnet test tests/Rig.TUnit.Architecture.Tests --filter "FullyQualifiedName~OrphanFolderTests"
# expect zero exit

git commit -m "feat(005): T002 — GREEN delete 3 stale orphan folders (FR-012)"

# Continue with T003..T008 per plan.md Phase 1
```

---

## 5. Phase 3 per-provider recipe (example: `Rig.TUnit.Core` missing Integration + Contract)

```bash
git checkout feat/005-a-legacy-coverage-and-tests

# ─── RED commit ────────────────────────────────────────────────────────────
# Create the Integration test project
dotnet new classlib -n Rig.TUnit.Core.Tests.Integration -o tests/Rig.TUnit.Core.Tests.Integration -f net10.0

# Wire TUnit + project reference + slnx registration
# (Copy pattern from existing tests/Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration/*.csproj)

# Add a failing test
cat > tests/Rig.TUnit.Core.Tests.Integration/RigBuilderIntegrationTests.cs <<'EOF'
namespace Rig.TUnit.Core.Tests.Integration;

public sealed class RigBuilderIntegrationTests
{
    [Test]
    public async Task Build_EndToEnd_ResolvesRigWithEveryConnectionSource()
    {
        // Intentionally written to fail against current Core surface;
        // GREEN commit will flesh this out
        Assert.Fail("RED: Core integration-test baseline not implemented yet (T020)");
    }
}
EOF

# Register in slnx
# (edit Rig.TUnit.slnx to add <Project Path="tests/Rig.TUnit.Core.Tests.Integration/Rig.TUnit.Core.Tests.Integration.csproj" />)

# Verify RED
dotnet build Rig.TUnit.slnx
dotnet test tests/Rig.TUnit.Core.Tests.Integration
# expect non-zero

git add tests/Rig.TUnit.Core.Tests.Integration Rig.TUnit.slnx
git commit -m "test(005): T020 — RED for Rig.TUnit.Core Integration + Contract"

# ─── GREEN commit ──────────────────────────────────────────────────────────
# Fill in the real test(s) — use the real public surface (RigBuilder, IRigConnectionSource, etc.)
# Example shape:
cat > tests/Rig.TUnit.Core.Tests.Integration/RigBuilderIntegrationTests.cs <<'EOF'
namespace Rig.TUnit.Core.Tests.Integration;

public sealed class RigBuilderIntegrationTests
{
    [Test]
    public async Task Build_EndToEnd_ResolvesRigWithEveryConnectionSource()
    {
        using var rig = new RigBuilder()
            .WithIsolationKey(IsolationKey.FromExecutionContext())
            .Build();

        await Assert.That(rig.IsolationKey.Value).IsNotEmpty();
        await Assert.That(rig.IsDisposed).IsFalse();
    }

    // … add more integration scenarios per FR-031
}
EOF

# Add Contract test project similarly
# Remove Rig.TUnit.Core from TestCompletenessTests SkipUntilFixed list (lines 22-53)

# Verify GREEN
dotnet test tests/Rig.TUnit.Core.Tests.Integration
# expect zero
dotnet test tests/Rig.TUnit.Architecture.Tests --filter "FullyQualifiedName~TestCompletenessTests"
# expect zero

# Check coverage — FR-038 coverage-lifting tests if < 90/85
dotnet run --no-build -c Release --project tests/Rig.TUnit.Core.Tests.Unit \
  -- --coverage --coverage-output-format cobertura

git add tests/Rig.TUnit.Core.Tests.Integration tests/Rig.TUnit.Core.Tests.Contract \
       tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs
git commit -m "feat(005): T020 — GREEN implement Rig.TUnit.Core Integration + Contract (FR-031)"
```

---

## 6. Coverage collection command (cheat sheet)

Single-project cobertura output to default location:

```bash
dotnet run --no-build -c Release --project tests/Rig.TUnit.Caching.Memory.Tests.Unit \
  -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml
# → tests/Rig.TUnit.Caching.Memory.Tests.Unit/bin/Release/net10.0/TestResults/coverage.cobertura.xml
```

Merge all projects locally:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  "-reports:tests/**/bin/Release/net10.0/TestResults/**/coverage.cobertura.xml" \
  "-targetdir:./coverage-report" \
  "-reporttypes:Html;Cobertura;MarkdownSummaryGithub"
open coverage-report/index.html
```

**Never** use `dotnet test /p:CollectCoverage=true` — that's the `coverlet.msbuild` path and it's not supported under MTP.

---

## 7. Phase 6c per-family README recipe (example: SQL family, T139/T140)

> **Analyze-revision note**: Each Phase 6c family has a RED task ID and a GREEN task ID as consecutive numbers (T137/T138, T139/T140, T141/T142, ...). The Markdig gate landed earlier in Phase 6a (T123b/c/d), so these Phase 6c RED commits genuinely fail the tightened structural gate.


```bash
git checkout feat/005-b-docs-parity

# ─── RED commit ────────────────────────────────────────────────────────────
# Author template-only READMEs using docs/templates/PROVIDER_README_TEMPLATE.md
# (The template lands in Phase 6a T122/T123; reference it.)

for provider in MySql Oracle Postgresql SqlServer Sqlite; do
    cp docs/templates/PROVIDER_README_TEMPLATE.md src/Rig.TUnit.Databases.Sql.$provider/README.md
    # Replace placeholders minimally so section headings remain but content is "TODO"
done

# Verify RED against the (future, Phase 6d) structural gate — encoded as branch-local skip
# (the current > 100-chars gate still passes; the new Markdig gate doesn't exist yet)

git add src/Rig.TUnit.Databases.Sql.{MySql,Oracle,Postgresql,SqlServer,Sqlite}/README.md
git commit -m "test(005): T139 — RED for SQL family README template scaffolds"

# ─── GREEN commit ──────────────────────────────────────────────────────────
# Fill each README with provider-specific research:
#  - Section 2 Purpose: Testcontainers.{Provider} + EF core wiring
#  - Section 3 When NOT: "use Sqlite for unit speed, MySQL for AUTO_INCREMENT fidelity, …"
#  - Section 5 Quick start: [Test] with UsingMySql().Build()
#  - Section 6 Configuration: table with ConnectionString, Username, Password, Database, Port
#  - Section 7 API surface: MySqlFixture, MySqlFixtureOptions, MySqlRigBuilder, UseMySql extension,
#    MySqlBuilderExtensions (EF wiring), MySqlQuirkTests
#  - Section 9 Provider quirks: AUTO_INCREMENT, utf8mb4 vs utf8, timestamp behaviour, Pomelo EF10 pin
#  - Section 10 Troubleshooting: Docker daemon, port conflicts, slow startup
#  - Section 11 Testing contracts: inherits SqlRigContract + ParallelIsolationContract
#  - Section 12 Performance: tests/Rig.TUnit.Benchmarks/MySqlBenchmarks.cs baseline 45ms cold / 2ms warm
#  - Section 13 Dependencies: Testcontainers.MySql, Pomelo.EntityFrameworkCore.MySql, MySqlConnector
#  - Section 14 Spec: FR-013 → .dotnet-ai-kit/features/004-provider-consistency-remediation/

git add src/Rig.TUnit.Databases.Sql.{MySql,Oracle,Postgresql,SqlServer,Sqlite}/README.md
git commit -m "feat(005): T140 — GREEN SQL family 5 READMEs against 14-section template (FR-065)"
```

---

## 8. Common pitfalls

| Pitfall | Symptom | Fix |
|---|---|---|
| Forgot RED commit | `commit-discipline-gate` fails at PR time | Rebase; insert the RED commit; `git push --force-with-lease` |
| RED commit secretly passes | `red-commit-verification` fails | The test uses assertions that don't actually fail. Tighten the assertion (e.g., `Assert.Fail` at minimum). |
| Introduced `[Skip]` | Grep audit fails on every PR | Remove the attribute; fix the test's isolation instead (ephemeral DB, unique key, per-test artefact). |
| Added `coverlet.msbuild /p:CollectCoverage` | Coverage output missing | Switch to `-- --coverage --coverage-output-format cobertura` — the MTP-native flag. |
| Retry the CI job on a flake | C-001 forbids retries | Reproduce locally; find the race; convert to per-test isolation; add the fix + audit entry in one PR. |
| Phase 3 partial fill-in | `TestCompletenessTests` still skips the provider | All four categories must land in one task. If Benchmark is hard, it still ships in the same commit — use a minimal `[Benchmark] [MemoryDiagnoser] public void Noop() { }` placeholder? **No.** Write a real minimal benchmark of `Fixture.InitializeAsync`. |
| README Quick start references deleted type | `snippet-extraction` gate fails | Regenerate the snippet against current API; add a RED test for the regression. |

---

## 9. When you're stuck

Files to read, in priority order:

1. [spec.md](spec.md) — the contract (FRs, SCs)
2. [plan.md](plan.md) — phase structure + task sequencing
3. [research.md](research.md) — evidence for each decision
4. [data-model.md](data-model.md) — schema of every artefact
5. [`.dotnet-ai-kit/features/004-provider-consistency-remediation/plan.md`](../004-provider-consistency-remediation/plan.md) — precedent
6. [`.dotnet-ai-kit/features/004-provider-consistency-remediation/handoff.md`](../004-provider-consistency-remediation/handoff.md) — lessons learned
7. [`planning/post-004-remediation/*`](../../../planning/post-004-remediation/) — source research

If none of these unblock: open a GitHub issue tagged `feature-005` with the task ID (TNNN) and the specific failure or decision point. Never guess your way past a RED commit.

---

## 10. Done definition

Feature 005 is "done" when:

- [x] All 7 phases complete per their exit gates (plan.md)
- [x] All 19 SCs pass (spec.md § Success Criteria)
- [x] `grep -rn "SkipUntilFixed" tests/` returns zero matches
- [x] All 4 architecture rules (TestCompleteness, ProviderCompleteness, TestFileOrganization, ReadmeCompleteness) enforce uniformly
- [x] Coverage gate blocks PRs below 90/85
- [x] Benchmark regression gate blocks PRs > 20 % regression
- [x] `commit-discipline-gate` + `red-commit-verification` pass on every PR
- [x] All 63 src READMEs pass `ReadmeCompletenessTests` (14-section `Markdig` gate)
- [x] Root has `LICENSE + CONTRIBUTING + SECURITY + CHANGELOG + README`
- [x] `docs/templates/`, `docs/QUALITY-BAR.md`, `docs/adr/` (8 ADRs), `docs/glossary.md`, `docs/troubleshooting.md`, `docs/performance-tuning.md`, `docs/migration-001-to-004.md` all present
- [x] `benchmarks/baseline-005.json` + `benchmarks/coverage-baseline-005.json` present
- [x] `/dotnet-ai-kit:review` returns PASS (or documented accepted advisories)
- [x] Final test count > 1264 (post-004 baseline)

Merge 005-a → master, merge 005-b → master, tag `v005`, close.
