# Documentation Audit

**Date:** 2026-04-19
**Scope:** root governance docs, per-project READMEs, planning/feature docs, contributor onboarding.

## 1. Solution-level — score 4/10

Root [`README.md`](../../README.md) is 22 lines. Coverage limited to:
- One-line description
- `git config core.hooksPath .githooks` setup
- Folder layout note

### Missing root governance files

| File | Status | Priority |
|---|---|---|
| `LICENSE` | Absent | P0 (blocks OSS release) |
| `CONTRIBUTING.md` | Absent (only `src/Rig.TUnit/Contributing-ProviderTemplate.md` buried deep) | P0 |
| `SECURITY.md` | Absent | P0 |
| `CHANGELOG.md` | Absent | P1 |
| `CODE_OF_CONDUCT.md` | Absent | P2 |

### Missing root README sections

- Project purpose beyond the one-liner
- Feature matrix (what families, which providers)
- Architecture overview / diagram
- Install / quick-start
- Links to per-provider READMEs
- Links to `Contributing-ProviderTemplate.md`
- API stability / versioning policy
- Roadmap and vision
- Supported .NET versions (currently implied by `global.json` / `Directory.Build.props`)
- CI status badge + coverage badge
- Link to the `.dotnet-ai-kit/features/` SDD history

## 2. Per-project READMEs

| Classification | Count | Range |
|---|---|---|
| EXCELLENT (> 2000 chars) | 2 | 2034–2104 B |
| GOOD (500–2000 chars) | 49 | 500–2000 B |
| MINIMAL (100–500 chars) | 2 | 474, 515 B |
| STUB (< 100 chars) | 0 | — |
| MISSING | 12 | 0 |

**Total:** 51 of 63 src projects have a README.

### 12 src projects missing README

- `src/Rig.TUnit/` (entry-point package)
- `src/Rig.TUnit.All/` (meta-package)
- `src/Rig.TUnit.Ci/`
- `src/Rig.TUnit.Core/`
- `src/Rig.TUnit.Grpc/`
- `src/Rig.TUnit.Mediator/`
- `src/Rig.TUnit.Microservices/` (base)
- `src/Rig.TUnit.Microservices.Contracts/`
- `src/Rig.TUnit.Microservices.Saga/`
- `src/Rig.TUnit.Parallelism/`
- `src/Rig.TUnit.Storage/` (base)
- `src/Rig.TUnit.WebAPI/`

### 2 minimal READMEs needing expansion

- `src/Rig.TUnit.Messaging/README.md` (474 bytes)
- `src/Rig.TUnit.Databases.NoSql/README.md` (515 bytes)

### Best-in-class (use as template source)

- `src/Rig.TUnit.Databases.Sql.MySql/README.md` (2104 B)
- `src/Rig.TUnit.Microservices.Outbox/README.md` (2034 B)
- `src/Rig.TUnit.Security.Jwt/README.md` (1095 B)
- `src/Rig.TUnit.Caching.Redis/README.md` (908 B)

## 3. Canonical per-project README template

Extracted from the good/excellent READMEs:

```markdown
# Rig.TUnit.{Family}.{Provider}

One-paragraph purpose. Describe what this package offers and its unique value vs
alternatives (e.g., "in-memory channel vs Testcontainers container").

## Install

```bash
dotnet add package Rig.TUnit.{Family}.{Provider}
```

## Quick start

```csharp
[Test]
public async Task Uses_{Provider}_in_a_test()
{
    using var rig = new RigBuilder()
        .Use{Provider}(o => { /* options */ })
        .Build();

    // exercise rig.{Provider} ...
}
```

## Options

| Property | Required | Default | Notes |
|---|---|---|---|
| `ConnectionString` | Yes | — | Bound from configuration via `SectionName` |
| `{Other}` | No | — | |

Config section: `Rig:TUnit:{Family}:{Provider}`.

## Helpers

List of helper types shipped (e.g., `SasBuilder`, `QuirkTests`, `Assert`).

## Dependencies

- Direct package references (Testcontainers pin, SDK versions)

## Spec reference

Implemented per [Feature 00X](../../.dotnet-ai-kit/features/00X-*/spec.md).
```

Canonical length: 500–1100 bytes for leaf providers. Base / meta packages can be shorter (200–500 B) but MUST exist and MUST be > 100 chars (per `ReadmeCompletenessTests`).

## 4. Contributing-ProviderTemplate.md — EXCELLENT

[`src/Rig.TUnit/Contributing-ProviderTemplate.md`](../../src/Rig.TUnit/Contributing-ProviderTemplate.md) exists and is production-quality:
- Full canonical file layout
- 7+ copy-paste-ready code examples
- Links to 3 canonical providers
- Architecture-test assertions referenced
- Options validation patterns shown

**Issue:** This document is buried under `src/Rig.TUnit/` rather than linked from the root README or a root `CONTRIBUTING.md`. New contributors will not find it.

## 5. Planning / feature docs (`.dotnet-ai-kit/features/`)

Four feature folders exist:

| Feature | Title | Status |
|---|---|---|
| 001 | Rig.TUnit Testing Infrastructure Library | Historical (shipped) |
| 002 | Rig.TUnit Fluent Builder Expansion | Historical (shipped) |
| 003 | Rig.TUnit Ecosystem Expansion | Historical (shipped) |
| 004 | Rig.TUnit Provider Consistency Remediation | Merged 2026-04-18 (PR #3) |

Plus `planning/` folders covering the same four features (this folder becomes the 5th).

**Classification:** these are internal spec-driven-development artefacts, not consumer-facing docs. FR IDs (e.g., "FR-030") and task IDs (e.g., "T172") are not explained in public docs — they presume SDD context. Value for library users is minimal.

## 6. Recommendations by priority

### P0 — Block OSS release

1. Add `LICENSE` (proposed: MIT, match typical .NET OSS library)
2. Add `CONTRIBUTING.md` at root — top-level TDD/commit rules + link to `Contributing-ProviderTemplate.md`
3. Add `SECURITY.md` at root — vulnerability disclosure channel
4. Rewrite root `README.md`:
   - Purpose + value prop
   - Feature matrix (families × providers, link to each README)
   - Install quick-start showing `Rig.TUnit.All` usage
   - Architecture diagram
   - CI badge + coverage badge (once coverage lands)
   - Links to `CONTRIBUTING.md`, `LICENSE`, `SECURITY.md`

### P1 — High impact

5. Fill the 12 missing per-project READMEs using the canonical template
6. Expand the 2 minimal READMEs (Messaging base, Databases.NoSql base)
7. Add `CHANGELOG.md` documenting 001–004 releases with breaking changes (notably the EventStore → KurrentDb rename in 004)
8. Remove `SkipUntilFixed` markers from `ReadmeCompletenessTests` once all 63 are covered

### P2 — Nice-to-have

9. Architecture Decision Records under `docs/adr/`:
   - ADR-001: Why Testcontainers over Docker Compose primary
   - ADR-002: Why CRTP `RigBuilder<TSelf>` pattern
   - ADR-003: Why Options pattern with SectionName
   - ADR-004: Why TUnit over xUnit/NUnit/MSTest
   - ADR-005: Why family-level contract tests over per-provider contract files
   - ADR-006: KurrentDB rename (Feature 004 Phase 1)
10. Architecture diagram (Mermaid) showing family graph + 60-provider matrix
11. Troubleshooting guide: container startup timeouts, network conflicts, Docker daemon issues
12. Glossary: Fixture, Rig, Contract, Stampede, Backplane, IsolationKey, TUnit terminology
13. Performance tuning guide: when to use which cache/storage/db provider
14. Migration guide: version upgrade path between 001→002→003→004

## 7. Effort estimate

| Item | Effort |
|---|---|
| Root README rewrite + LICENSE + CONTRIBUTING + SECURITY | 4–6 h |
| Fill 12 missing per-project READMEs | 6–8 h (base/meta READMEs are short) |
| Expand 2 minimal READMEs | 1 h |
| CHANGELOG.md with 001–004 history | 2–3 h |
| 6 ADRs | 4–6 h |
| Architecture diagram + feature matrix | 2 h |
| Glossary + troubleshooting + tuning guides | 4 h |
| Remove `SkipUntilFixed` from `ReadmeCompletenessTests` | 15 min (post the above) |

**Total: ~25–35 hours** for a thorough documentation pass.
