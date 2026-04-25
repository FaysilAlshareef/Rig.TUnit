# Requirements Quality Checklist — 007-messaging-topology-sessions

Generated: 2026-04-23

Authoritative spec (single source of truth): [spec.md](../spec.md)
Design inputs (not a spec — reference only): [planning/messaging-topology-and-sessions/](../../../../planning/messaging-topology-and-sessions/)

## Specification Quality

- [x] User stories have priorities (P1, P2, P3) — 5 stories (P1: 2, P2: 2, P3: 1)
- [x] Each user story is independently testable
- [x] Each user story has ≥ 1 acceptance scenario in Given/When/Then form
- [x] Maximum 3 `[NEEDS CLARIFICATION]` markers — 0 present (5 open questions have proposed resolutions)
- [x] Requirements are testable and verifiable by artefact or CI output
- [x] Key entities identified: `SendContext`, `ITopologyBuilder`, per-provider topology builders, session-aware listeners, `NatsJetStreamFixture`, `CapturedMessage<T>.SessionKey`
- [x] Edge cases documented (7 edge cases)
- [x] Success criteria are measurable (6 SC items with artefact-level thresholds)

## Functional Requirements

- [x] FR-007-01 — per-provider `SendContext` sender overload — validated via unit + integration tests per provider
- [x] FR-007-02 — `WithTopology` hook on every `{Provider}RigBuilder` — validated via unit (mock SDK) + integration (live container) tests per provider
- [x] FR-007-03 — `OrderingAssert.PerKeyMonotonic` green end-to-end on all 5 providers — validated via 5 CI integration matrix runs
- [x] FR-007-04 — zero regression vs `master` baseline — validated via CI green delta
- [x] FR-007-05 — coverage ≥ 90 % line / ≥ 85 % branch per affected package — validated via Codecov / coverage-scan gate
- [x] FR-007-06 — ServiceBus admin client on emulator ≥ 1.1.2 — validated via T014 capability probe + Phase 1 exit gate
- [x] FR-007-07 — `NATS.Client.JetStream` referenced only by the Nats package — validated via extended `DependencyDirectionTests`
- [~] ~~FR-007-08 — additive-only public API~~ — **superseded by C-000** (packages pre-release; breaking changes allowed when they yield a cleaner surface; `dotnet api-diff` not run as a gate)

## Non-Functional Requirements

- [x] NFR-C1 — coverage gate thresholds specified (≥ 90 % line / ≥ 85 % branch); new public types 100 % line-covered in their introducing PR
- [x] NFR-C2 — no public-API breaking change; all new parameters optional
- [x] NFR-C3 — docs shipped in the same PR as code; mandatory file list enumerated (README, 5 provider docs, CHANGELOG, ordering-assertions.md, inline XML)
- [x] NFR-C4 — provider-parity architecture test (`ProviderCompletenessTests`) lands RED in T003 and goes green per provider phase
- [x] NFR-C5 — Phase 6 benchmarks populated (ServiceBus session vs non-session; Kafka multi-partition per-key)

## Architecture Constraints

- [x] .NET version detected: `net10.0` / C# 14
- [x] Affected layers identified: Base library (`Rig.TUnit.Messaging`), 5 provider packages, `Architecture.Tests`, `Rig.TUnit.Benchmarks`, docs
- [x] Dependency direction preserved: `Messaging.{Provider}` → `Messaging` → `Core`; no new inter-provider edges
- [x] Reference implementations identified: existing `ProviderCompletenessTests` pattern, `MessagingFixtureBase`, `OrderingAssert.PerKeyMonotonic`, Feature 006 coverage gate
- [x] No cross-family project references (e.g., Messaging does not start referencing Storage / Databases)
- [x] No library swaps — TUnit / Azure.Messaging.ServiceBus (≥ 7.20.1 bump is additive) / Confluent.Kafka / RabbitMQ.Client / NATS.Client (+ new `NATS.Client.JetStream` in Nats package only) / AWSSDK.SQS / BenchmarkDotNet remain unchanged in role
- [x] `NATS.Client.JetStream` is the only new external dependency; scoped to `Rig.TUnit.Messaging.Nats.csproj` and gated by FR-007-07 architecture test

## TDD Discipline

- [x] RED → GREEN commit discipline specified per task (see [spec.md §Task List](../spec.md))
- [x] Single-GREEN rationale explicit for test-only / config-only tasks (T014, T015, T016, T025, T033, T044, T055, T060, T062)
- [x] Commit message prefixes defined: `test(007): RED …`, `feat(007): GREEN …`, `fix(007): GREEN …`
- [x] `--amend` across RED/GREEN boundary explicitly prohibited (matches `.claude/rules/multi-repo.md`)
- [x] `--no-verify` / `--no-gpg-sign` explicitly prohibited
- [x] No destructive git operations (`reset --hard`, `push --force`, branch deletion) without explicit user approval

## Phase Ordering

- [x] Phase 0 (base library, T000–T003) is BLOCKING — every provider phase depends on it
- [x] Phases 1, 2, 3, 4, 5 are parallel-eligible after Phase 0 exit gate
- [x] Phase 6 (T060–T063) is LAST — docs and benchmarks consolidate after every provider phase ships
- [x] T003 provider-parity test is RED until each provider's Phase N lands; RED status tracked in PR description

## Coverage Plan

- [x] Per-package baseline sourced from Feature 006 scan run `24712477011` (raw pre-uplift numbers in Feature 006 spec)
- [x] Per-package target: ≥ 90 % line / ≥ 85 % branch (Feature 006 exit gate)
- [x] Every new public type listed in `Sessions-And-Partitions-Design.md` and `Topology-Builder-Design.md` appears in the coverage-plan table
- [x] Each coverage row names the tests that close the gap

## Documentation

- [x] `README.md` — tasks: T000, T002, T023, T031, T042, T054, T060
- [x] `docs/providers/service-bus.md` (create) — tasks: T010, T011, T012, T013, T014, T015, T016, T062
- [x] `docs/providers/kafka.md` — tasks: T020, T021, T022, T023, T024, T025, T062
- [x] `docs/providers/rabbitmq.md` (create) — tasks: T040, T041, T042, T043, T044
- [x] `docs/providers/nats.md` (create) — tasks: T050, T051, T052, T053, T054, T055
- [x] `docs/providers/sqs.md` (create) — tasks: T030, T031, T032, T033
- [x] `CHANGELOG.md` — task: T061 (one entry per shipped phase, not batched)
- [x] `docs/ordering-assertions.md` (create or update) — tasks: T001, T063
- [x] Inline XML docs on every new public type and every new public parameter — applied to every production GREEN task

## Risks

- [x] All 8 risks documented with likelihood, impact, and mitigation (5 from roadmap + 3 spec-level)
- [x] High-impact risks have concrete mitigations: R1 (ServiceBus emulator probe), R5 (parity test), R6 (`CapturedMessage` naming kept stable)

## Planning References

- [x] All 6 planning documents listed with their role in the SDD spec
- [x] Ground-truth data sources identified (`coverage-scan-results/summary.csv` from run `24712477011`)
- [x] Commit-discipline rule source cited (`.claude/rules/multi-repo.md`)

## Items Requiring No Clarification

The following were judged to have clear defaults and were NOT marked `[NEEDS CLARIFICATION]`:

| Item | Default Applied |
|------|-----------------|
| Coverage gate thresholds | ≥ 90 % line / ≥ 85 % branch (Feature 006 exit gate) |
| Commit prefixes | `test(007): RED …` / `feat(007): GREEN …` / `fix(007): GREEN …` (from HARD CONSTRAINT) |
| Branch name | `feat/007-messaging-topology-sessions` (from `.claude/rules/multi-repo.md`) |
| Benchmark project | Extend existing `tests/Rig.TUnit.Benchmarks/` (see open question Q-4) |
| `CapturedMessage<T>` envelope property name | Keep `Message` (see open question Q-1) |
| Non-supported `ITopologyBuilder` structural methods | `NotSupportedException` with hint (from Topology-Builder-Design.md) |
| Non-supported `With…` config methods | No-op with traceable signal (see open question Q-2) |

## Sign-off

- Spec authoritative contract: [spec.md](../spec.md) (single source of truth)
- Design inputs (reference only, not specs): [planning/messaging-topology-and-sessions/](../../../../planning/messaging-topology-and-sessions/)
- Clarifications resolved: C-000 (pre-release), C-001 (`CapturedMessage<TMessage>` shape), C-002 (per-scenario RED+GREEN).
- Open questions Q-3 / Q-4 / Q-5 remain; user review **before** `/dotnet-ai-kit:plan`.
