# ADR-009 — Feature numbering after Feature 007

**Status**: Accepted
**Date**: 2026-04-25
**Deciders**: Faysil Alshareef
**Supersedes**: —
**Superseded by**: —
**Related**: [docs/ROADMAP.md](../ROADMAP.md), `feedback_spec_home_is_sdd_feature_folder.md` (memory)

---

## Context

Features 001–006 shipped. Feature 007 (`messaging-topology-sessions`) is in Phase 6. The 2026-04-25 gap analysis identified 43 additional Feature-007-magnitude features (F-008 through F-050) covering cross-cutting test primitives, SQL & NoSQL admin / consistency / cost surfaces, caching correctness, storage lifecycles, security attack scenarios, observability propagation, microservices correctness, HTTP/gRPC protocol depth, and platform concerns (HealthChecks / Resilience / Concurrency / WebAPI / CI).

This ADR formalises:
1. how those 43 IDs are assigned,
2. where each lives during its **planned** vs **specced** vs **shipped** stages,
3. the rule that prevents `planning/` from sprouting parallel specs.

## Decision

### 1. Numbering scheme

- Features are numbered with a **monotonic, three-digit, zero-padded** ID: `008`, `009`, …, `050`, … `999`.
- IDs are **never reused**. A cancelled feature keeps its ID and is marked `cancelled` in [docs/ROADMAP.md](../ROADMAP.md). The next feature still takes the next ID, not the cancelled one.
- The block 008–050 is **reserved by this ADR** for the gap analysis dated 2026-04-25. Any new feature discovered before 050 ships still takes the next free ID (≥ 051) — we do not slot newcomers into the reserved block.
- Branch name follows the existing `feat/{NNN}-{slug}` convention (e.g. `feat/008-deterministic-clock`), matching `.claude/rules/multi-repo.md`.

### 2. Stage → location mapping

| Stage | Owns | File / folder | Created by |
|-------|------|---------------|------------|
| `planned` | Brief, motivation, gap inventory, dependencies, build prompt | `planning/<slug>/README.md` | Manually, in a roadmap PR |
| `specced` | Full spec.md, plan.md, tasks.md, etc. | `.dotnet-ai-kit/features/NNN-<slug>/` | `/dai.spec` reading the planning README |
| `in-progress` | RED+GREEN commits per task | `feat/NNN-<slug>` branch | `/dai.implement` |
| `verifying` | verify.md, review.md | `.dotnet-ai-kit/features/NNN-<slug>/` | `/dai.verify` |
| `shipped` | CHANGELOG entry, tag | `CHANGELOG.md` + git tag | merge to `master` |

### 3. Single-source-of-truth rule

- The full **specification** of any feature lives in **exactly one** place: `.dotnet-ai-kit/features/NNN-<slug>/spec.md` once the feature is specced.
- The `planning/<slug>/README.md` brief is an **input** to `/dai.spec` and may be **stale relative to the spec** once the spec exists. Brief readers should treat the spec as authoritative the moment status flips to `specced`.
- This continues the rule recorded in `feedback_spec_home_is_sdd_feature_folder.md`: never create a parallel `Feature-NNN-Spec.md` under `planning/`.

### 4. ADR coverage

Cross-cutting decisions that span ≥ 3 of the 43 features get their own ADR (ADR-010 onward). Examples likely to need ADRs:

- **ADR-010** (planned) — `TimeProvider`-based fake clock as the foundation primitive. Decided once for F-008; every later feature that touches time inherits the choice.
- **ADR-011** (planned) — Fault-injection sidecar (Toxiproxy) vs. in-process delegating handlers. Decided once for F-009.
- **ADR-012** (planned) — W3C `traceparent` as the cross-fixture correlation key. Decided once for F-012; F-034/F-038/F-039 inherit.

ADRs are numbered with their own monotonic counter, separate from feature IDs.

## Consequences

### Positive

- Predictable navigation: every planned feature has a deterministic path (`planning/<slug>/README.md`); every specced feature has a deterministic path (`.dotnet-ai-kit/features/NNN-<slug>/`).
- No spec sprawl: planning briefs are short (≤ 200 lines), so 43 of them is ~6–8 k lines of markdown — small enough to grep, big enough to convey real intent.
- Dependency graph is explicit (in `Depends on` columns of the roadmap), so a future PR can add a `RoadmapCompletenessTests` architecture test that asserts every shipped feature's dependencies were also shipped before it.

### Negative

- Extra step on pickup: copying the planning README's "Build prompt" section into `/dai.spec`. Mitigated by keeping the build prompt as the **last** section of every brief.
- Stale-brief risk: once a feature is specced, the brief can drift from reality. Mitigated by the single-source-of-truth rule and a `Status` field on the brief itself.
- Reserved-block convention is fragile: if F-051 lands while F-020 is still planned, the roadmap looks "out of order". Accepted — the order in the roadmap table (which groups by family) is what matters for readers, not the numeric order.

### Neutral

- This ADR does not change the SDD pipeline (`/dai.spec` → `/dai.plan` → `/dai.tasks` → `/dai.implement` → `/dai.verify` → `/dai.pr`); it only adds a planning-stage entry point upstream of `/dai.spec`.
- This ADR does not specify any GitHub Issues / Milestones / Project layer — that's a separate convention (`docs/ROADMAP.md` is the markdown source of truth; GitHub artefacts are an optional sync target).

## Alternatives considered

### A — Spec all 43 upfront via `/dai.spec`

Rejected. The Spec-Kit guidance (Microsoft, 2025) is that specs are sized for 1–5 days of work; 43 dormant specs would dilute the signal of "ready to plan" and force re-specs every time an upstream cross-cut feature lands.

### B — GitHub Issues only (no `planning/` briefs)

Rejected. The repo already encodes design inputs in `planning/<topic>/`; switching to issues-only would split the source of truth and break offline workflows. A future PR can sync `docs/ROADMAP.md` ↔ GitHub Issues; that's additive, not a replacement.

### C — Top-level `ROADMAP.md` only (no per-feature briefs)

Rejected. A single file with full context for 43 features balloons past 5 000 lines and cannot be edited without merge conflicts when multiple features are picked up in parallel. Per-feature files give us per-feature PR ownership.

### D — Reuse cancelled IDs

Rejected. ID reuse silently breaks every external reference (CHANGELOGs, ADRs, commit messages) that pointed at the cancelled work. The cost of "wasted" IDs is zero; the cost of stale references is non-zero. Monotonic IDs win.

## Notes

This ADR is itself a planning artefact, not a feature. It does not need its own SDD spec.
