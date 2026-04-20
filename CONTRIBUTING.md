# Contributing to Rig.TUnit

Thank you for contributing. This guide captures the hard rules every PR MUST satisfy.

## TDD discipline (FR-001/FR-002/FR-003)

Every `src/`-touching change lands as TWO commits in strict order:

```
Commit A   test(005): TNNN — RED for {summary}
             → test files only
             → build + test exit non-zero at this SHA

Commit B   feat(005): TNNN — GREEN implement {summary}
             → production change
             → all tests added in Commit A now pass
```

Forbidden:
- **No new** `[Category("SkipUntilFixed")]`, `[Skip]`, or `[NotInParallel]` markers
  anywhere in `tests/**/*.cs`. Use per-test isolation helpers instead.
- **No** `coverlet.msbuild` — use the MTP-native `-- --coverage` path.
- **No** CI retries on matrix failures. Red is red.

## Coverage gate (FR-020-FR-024)

- **line-rate ≥ 0.90**
- **branch-rate ≥ 0.85**

Collect locally:

```sh
dotnet test tests/Rig.TUnit.<Provider>.Tests.Integration/ \
    --no-build -c Release \
    -- --coverage \
       --coverage-output-format cobertura \
       --coverage-output coverage.cobertura.xml
```

Merge:

```sh
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
    "-reports:./tests/**/TestResults/**/coverage.cobertura.xml" \
    "-targetdir:./coverage-report" \
    "-reporttypes:Html;Cobertura;MarkdownSummaryGithub"
```

The `coverage-summary` CI job does this automatically on every PR and blocks merge
on threshold breach (flipped to blocking at Phase 3 close via T069b).

## The full CI gate set (FR-074)

Every PR must pass all of the following:

| Job | Purpose | Blocking | FR |
|---|---|---|---|
| `build-unit-arch` | Compile + run unit/arch/contract tests | Yes | FR-030-031 |
| `integration-{sql,nosql,caching,messaging,microservices,security,observability,storage,core}` | 9 matrix jobs running integration tests with `--coverage` | Yes (matrix cells fail-fast: false) | FR-020 |
| `coverage-summary` | Merge cobertura + enforce `line ≥ 0.90 / branch ≥ 0.85` | Yes (from T069b onwards) | FR-021, FR-022 |
| `architecture-tests` | Dedicated arch-rule run (Phase 7 T164) | Yes | FR-070 |
| `benchmark-regression` | 20% budget vs `baseline-005.json` (path-filtered) | Yes on src/benchmarks changes | FR-071, FR-037 |
| `commit-discipline-gate` | RED→GREEN subject pairing check (Phase 7 hardened) | Yes on PR | FR-002 |
| `red-commit-verification` | Every RED commit verified non-zero at its SHA | Yes on PR | FR-003 |
| `markdown-link-check` | Dead-link scan across all `.md` | Yes on PR | FR-067 |
| `snippet-extraction` | Build every leaf README's `## Quick start` block | Yes on src-touching PRs | FR-068 |

## Adding a new provider

Every leaf provider ships the canonical quartet:

1. `src/Rig.TUnit.{Family}.{Provider}/Fixtures/{Provider}Fixture.cs`
2. `src/Rig.TUnit.{Family}.{Provider}/Options/{Provider}FixtureOptions.cs`
   (with `public const string SectionName = "RigTUnit:{Provider}"`)
3. `src/Rig.TUnit.{Family}.{Provider}/Builder/{Provider}RigBuilder.cs`
4. `src/Rig.TUnit.{Family}.{Provider}/Builder/{Provider}RigBuilderExtensions.cs`
   with a public static `Use{Provider}(this RigBuilder, …)` method.

The architecture rule [`ProviderCompletenessTests`](tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs)
enforces this at CI time.

### Also required

- `tests/Rig.TUnit.{Family}.{Provider}.Tests.Unit/` project
- `tests/Rig.TUnit.{Family}.{Provider}.Tests.Integration/` project
- `{Provider}Contract.cs` in Integration, inheriting the family contract suite
- `{Provider}*Benchmarks.cs` in `tests/Rig.TUnit.Benchmarks/`
- `src/Rig.TUnit.{Family}.{Provider}/README.md` matching the
  [canonical template](docs/templates/PROVIDER_README_TEMPLATE.md)

The architecture rule [`TestCompletenessTests`](tests/Rig.TUnit.Architecture.Tests/Rules/TestCompletenessTests.cs)
enforces the test-pyramid shape.

## README rubric

Every provider README is scored against [`docs/QUALITY-BAR.md`](docs/QUALITY-BAR.md)
on a Pass / Needs work / Missing scale. Merging requires:

- 0 sections graded **Missing**
- ≤ 2 sections graded **Needs work**
- §5 (Quick start) graded **Pass** — snippet must compile

`ReadmeCompletenessTests` + `snippet-extraction` enforce this mechanically.

## Shared fixtures

`tests/**/Shared*Fixture.cs` files must carry an `Intentional reuse per 004/005 edge
case: <reason>` rationale comment explaining why sharing is safe. Enforced by
[`SharedFixtureGuardTests`](tests/Rig.TUnit.Architecture.Tests/Rules/SharedFixtureGuardTests.cs).

## Dependency direction

Family-base packages depend only on `Rig.TUnit.Core`. Leaf providers depend on their
family base. Tests reference Rig.TUnit.* packages. **Never** create a leaf → leaf
reference — enforced by `DependencyDirectionTests`.

## PR checklist

Before opening a PR:

- [ ] Each `src/`-touching change lands as a RED commit followed by a GREEN commit
- [ ] New providers ship the canonical quartet + the full test pyramid
- [ ] New public types have unit tests
- [ ] New `Shared*Fixture.cs` files carry an `Intentional reuse` comment
- [ ] New READMEs follow the 14-section template
- [ ] Coverage stays ≥ 0.90 / 0.85
- [ ] `dotnet test tests/Rig.TUnit.Architecture.Tests` passes locally

## See also

- [Architecture diagram](docs/architecture-diagram.md)
- [Glossary](docs/glossary.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Performance tuning](docs/performance-tuning.md)
- [Migration 001 → 004](docs/migration-001-to-004.md)
- [ADRs](docs/adr/)
- [Third-party notices](docs/third-party-notices.md)
