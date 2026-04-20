# Post-004 Remediation — Planning Folder

Captures outstanding issues discovered in the codebase review on **2026-04-19** after Feature 004 (provider-consistency-remediation) merged to `master` via PR #3 (commit `9d3369f`).

Scope: this folder is **research + remediation planning only** — nothing here changes code. It feeds the eventual Feature 005 spec.

## What's here

| File | Purpose |
|---|---|
| [CI-Postgres-Flake-RCA.md](CI-Postgres-Flake-RCA.md) | Root-cause analysis of the single failing CI job on `master` — `Integration — SQL matrix (Postgresql)`. Explains why this is a shared-fixture race, not a real regression. |
| [Test-Coverage-Gap-Matrix.md](Test-Coverage-Gap-Matrix.md) | FR-030 four-category audit (Unit + Integration + Contract + Benchmark) across all 63 src projects — lists every pre-004 project that falls short. |
| [Project-Organization-Audit.md](Project-Organization-Audit.md) | Structural audit: slnx completeness, orphan folders, canonical layout adherence, reference graph, architecture rules enforcement state. |
| [Documentation-Audit.md](Documentation-Audit.md) | Root governance docs, per-project README coverage, canonical README template, documentation priority list. |
| [CI-Artifact-And-Coverage-Proposal.md](CI-Artifact-And-Coverage-Proposal.md) | Concrete YAML proposal for uploading TUnit HTML reports + cobertura coverage per job, with retention and a merged summary job. |
| [Proposed-Feature-005-Roadmap.md](Proposed-Feature-005-Roadmap.md) | Seven-phase remediation plan (flake fix → coverage wiring → test fill-in → benchmarks → docs → cleanup → architecture rule enforcement). |

## Headline findings (TL;DR)

1. **CI flake (high severity)** — `UsePostgresFluentTests.UsePostgres_DbContext_PerformsInsertSelectRoundTrip` fails intermittently on a shared Postgres container because schema-creating tests run in parallel against one physical DB. Not a merge regression — same bug failed multiple times on the feat branch before the one green run that enabled merge.
2. **Test-category debt (medium severity)** — ~23 pre-004 projects violate FR-030's mandate that every provider ship Unit + Integration + Contract + Benchmark. 21 providers have no BenchmarkDotNet class at all.
3. **Coverage gate is unenforced (medium severity)** — FR-035/036 define ≥ 90% line / ≥ 85% branch gates, but `ci.yml` never emits cobertura. Gate exists only on paper.
4. **Architecture rules partially enforced (medium severity)** — `ProviderCompletenessTests`, `TestFileOrganizationTests`, `ReadmeCompletenessTests` exist but run with `[Category("SkipUntilFixed")]` markers for in-flight providers.
5. **Empty orphan folders (low severity)** — `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`, `tests/Rig.TUnit.SqlServer.Tests.Integration/` contain only `bin/obj/` and no code. Stale from pre-rename.
6. **Documentation gaps (high for OSS-readiness, scope revised 2026-04-19)** — no LICENSE, CONTRIBUTING.md, CHANGELOG.md, SECURITY.md, CODE_OF_CONDUCT.md at root. Root README is 22 lines. The original audit found 12 missing READMEs + 2 minimal expansions, but a quality re-audit (see [Documentation-Audit.md §2.2](Documentation-Audit.md)) confirmed that **all 51 existing READMEs — including the previously-EXCELLENT MySql and Outbox — fall below the new 14-section quality bar**. Effective scope: **all 63 src projects** need a README produced or rewritten against `docs/templates/PROVIDER_README_TEMPLATE.md`, plus `ReadmeCompletenessTests` must be tightened from `> 100 chars` to a structural section-presence gate. Effort grew from ~25–35 h to ~80–110 h; Phase 6 of the Feature 005 roadmap now runs as its own parallel sub-feature (`feat/005-b-docs-parity`).

## Related planning folders

- `planning/provider-consistency-remediation/` — Feature 004 (just merged); this folder is the immediate follow-up.
- `planning/ecosystem-expansion/` — Feature 003 baseline.
- `planning/fluent-builder-expansion/` — Feature 002 history.
- `planning/base-library/` — historical base library design.

## Next step

Once the issues in these documents are confirmed, promote [Proposed-Feature-005-Roadmap.md](Proposed-Feature-005-Roadmap.md) into a `.dotnet-ai-kit/features/005-*/` spec via `/dotnet-ai-kit:specify`.
