# Feature 003 — Requirements Quality Checklist

Review this before `/dai.clarify` and `/dai.plan`. Every item should be YES.

## User Stories

- [x] Each user story has a Priority (P1, P2, or P3).
- [x] Each user story is independently testable.
- [x] Each user story has concrete Acceptance Scenarios in Given/When/Then form.
- [x] User stories cover every hard requirement (R1–R10) and every phase (A–E).
- [x] TDD discipline is itself a first-class user story (US1).

## Functional Requirements

- [x] Every FR is testable (has an observable pass/fail condition).
- [x] Every FR maps to at least one User Story (see Traceability table).
- [x] Hard requirements R1–R10 each map to one or more FRs.
- [x] Rule-compliance FRs (`.claude/rules/*.md`) explicitly listed (FR-050..057).
- [x] Package-tree FRs cover all ~50 packages across 5 phases.

## Key Entities

- [x] Base contracts (`I{Area}Rig`, `{Area}FixtureBase`, `{Area}RigBuilder<TSelf>`, `{Area}Assert`) listed.
- [x] Shared helpers (`WaitHelper`, `ListenerBase`, `EventSenderBase`, `BackplaneCapture`, `StampedeTester`, `ClockControl`, `SeedBuilder`, `DbContextHelper`) listed.
- [x] Provider-specific fixtures enumerated per area.
- [x] Microservice harness entities (Outbox, Inbox, EventSourcing, Snapshots, Saga, Contracts) listed.

## Architecture Scope (generic mode)

- [x] Affected layers / directories table explicit.
- [x] Deletion list explicit (3 src + matching tests).
- [x] New-package list organized by Phase A–E.
- [x] Layer dependency direction stated; enforced by `Rig.TUnit.Architecture.Tests`.

## TDD Protocol

- [x] RED / GREEN / REFACTOR steps defined at method level.
- [x] Contract-first TDD defined for base areas.
- [x] Provider TDD defined (13 mandatory tests + 3 quirks).
- [x] Assertion-DSL TDD defined (5 required cases per assertion).
- [x] Commit-message prefixes (`test:`, `feat:`, `refactor:`) defined for traceability.

## Phased Delivery

- [x] Each phase has its own scope + merge gate.
- [x] Phase A covers hard cutover + base contracts.
- [x] Phase B covers observability + security + HTTP + resilience.
- [x] Phase C covers microservice patterns + concurrency + health + caching-memory.
- [x] Phase D covers provider expansion.
- [x] Phase E covers polish + remaining providers.

## Edge Cases

- [x] Docker unavailability handled (`[EnabledOnDocker]` filter).
- [x] Flaky emulators handled (quarantine / retry).
- [x] Version drift prevented (`Directory.Build.props` pins).
- [x] Coverage drift prevented (CI enforcer).
- [x] Shared mutable state detected (`Parallelism.SharedState.Detector`).
- [x] Port collisions prevented (allocator).
- [x] Snapshot `.received.*` CI fail-path documented.
- [x] InMemoryDb fidelity trap documented.

## [NEEDS CLARIFICATION]

- [x] Three markers total — all within the 3-item budget.
- [x] Each marker has a documented default to unblock planning if unresolved.
- [x] Resolved questions from handoff open-questions list are NOT marked (Postgresql naming, Caching.Memory scope, EventSourcing provider independence).

## Success Criteria

- [x] All SCs are measurable (coverage %, test count, zero warnings, CI matrix green).
- [x] SC for TDD cadence included (SC-011).
- [x] SC for merge gate included (SC-012).
- [x] SC for hard requirements R1–R10 traced in the Traceability table.

## Multi-Repo (N/A — generic mode)

- [x] N/A — single-repo library ecosystem. No briefs need projecting.

---

## Summary

- User stories: **13** (P1: 8, P2: 4, P3: 5)
- Functional requirements: **~50** (FR-001..FR-120 grouped)
- Success criteria: **15**
- `[NEEDS CLARIFICATION]` markers: **0 remaining** (5 resolved as C-001..C-005; C-006 added during analysis-fix pass for anti-pattern detector mechanism)
- Analysis findings: **17 resolved** (3 HIGH + 8 MEDIUM + 6 LOW — see [analysis.md](../analysis.md))
- Affected layers: **single-repo library**, ~50 new packages + 1 Roslyn analyzer across 5 phases, 3 packages deleted.
- TDD: **mandatory RED-GREEN-REFACTOR** documented at method, contract, provider, and assertion-DSL levels; enforced by T006 commit-msg hook.
