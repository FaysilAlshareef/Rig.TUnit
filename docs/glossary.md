# Rig.TUnit Glossary

Every term used in any provider README resolves here. When introducing new
terminology in a README, add it to this glossary first.

## Core concepts

- **Rig** — a composition of fixtures + helpers wired up via `RigBuilder`.
  Consumers build one Rig per test to stand up their test environment.
- **Fixture** — container-backed or in-process resource with an `InitializeAsync` +
  `DisposeAsync` lifecycle (e.g., `PostgresFixture`, `KafkaFixture`).
- **RigBuilder** — the CRTP-shaped fluent builder; every provider ships a subclass.
- **RigConnect** — static factory for `IRigConnectionSource` — wraps a raw value, an
  `IConfiguration` key, an `IOptions<T>` selector, or a smart container/config hybrid.
- **IsolationKey** — per-test unique name primitive (hash-derived); see
  [ADR-006](adr/ADR-006-isolationkey.md).

## Test-pyramid vocabulary

- **Contract suite** — abstract test class shared across every provider in a family
  (see [ADR-005](adr/ADR-005-family-level-contracts.md)). Derived via `[InheritsTests]`.
- **ParallelIsolationContract** — specific contract ensuring concurrent test execution
  doesn't cross-contaminate (every family inherits this).
- **QuirkTests** — provider-specific tests that document non-portable behaviour
  (MySql's case-insensitive identifiers, Oracle's session overhead).

## Messaging vocabulary

- **Listener** — `{Provider}Listener` helper that subscribes to a topic/queue/subject
  and captures messages for assertion.
- **Sender / EventSender** — `{Provider}EventSender` helper that publishes a test
  event with correlation IDs pre-populated.
- **Backplane** — pub/sub channel used for cache invalidation (Redis pub/sub).
- **W3C traceparent** — standard header propagated through messaging + HTTP so
  distributed traces stitch together.

## Observability vocabulary

- **IsolationKey for spans** — activity sources tag their spans with the IsolationKey
  so captured traces filter per-test.
- **TagCardinalityGuard** — `Rig.TUnit.Observability.Metrics` helper that fails tests
  emitting excessive distinct tag values.
- **Snapshot capture** — `Rig.TUnit.Observability.Seq` writes a JSON dashboard snapshot
  per test run for artefact archiving.

## Microservices vocabulary

- **Inbox pattern** — receiver-side idempotency via `SequenceTracker` rejecting
  duplicates + out-of-order events.
- **Outbox pattern** — sender-side exactly-once via a relay draining `OutboxMessage`
  rows into the messaging provider.
- **EventSender** (in Microservices context) — bridges the outbox to the bus.
- **SagaHarness** — orchestrates multi-step saga execution with deterministic
  `ResilienceClock` + timeout helpers.
- **Snapshot scrubbers** — `MicroserviceScrubbers` replaces GUIDs, timestamps,
  correlation IDs, and connection strings with placeholders for stable snapshots.

## CI + automation vocabulary

- **Commit-discipline gate** — CI job asserting every `feat(005)` GREEN commit is
  preceded by a matching `test(005)` RED commit.
- **Red-commit-verification** — Phase-7 CI step that checks out each RED commit and
  confirms it actually fails.
- **Coverage gate** — per-package line-rate ≥ 0.90 / branch-rate ≥ 0.85 enforced via
  the `coverage-summary` CI job.
- **Benchmark regression budget** — 20% deviation from `benchmarks/baseline-005.json`
  fails the `benchmark-regression` CI job.

## Miscellaneous

- **Shared*Fixture** — a static one-container-per-test-project helper. Safe only when
  consumers use per-test isolation primitives (audited in A005).
- **Intentional reuse** — rationale comment required on every `Shared*Fixture.cs`
  (enforced by `SharedFixtureGuardTests`).
- **Markdig gate** — `ReadmeCompletenessTests` structural parser that requires every
  leaf README to contain the 14-section canonical shape.
