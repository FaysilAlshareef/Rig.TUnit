# Feature Specification: Messaging Topology & Sessions

**Feature ID**: 007-messaging-topology-sessions
**Created**: 2026-04-23
**Status**: Ready for `/dotnet-ai-kit:plan` — 6 clarifications resolved, 0 remaining
**Branch**: `feat/007-messaging-topology-sessions`
**Depends on**: Feature 006 exit gates (≥ 90 % line / ≥ 85 % branch per source package) green on `master`.
**Providers in scope**: Azure Service Bus, Apache Kafka, RabbitMQ, NATS (JetStream), Amazon SQS (5/5 messaging families).

**Pre-release note (C-000)**: `Rig.TUnit.*` packages are **unreleased**. Any legacy "additive only" / "no public-API breaking change" clauses (originally NFR-C2 / FR-007-08) are **superseded** — every clean-vs-compatible fork resolves to *clean*. No `[Obsolete]` aliases, no forwarder properties, no dual shapes.

---

## Planning inputs (authoritative for scope, design, effort)

| Artefact | Path | Role |
|----------|------|------|
| Scope / file index | [planning/messaging-topology-and-sessions/README.md](../../../planning/messaging-topology-and-sessions/README.md) | What the feature is and why it exists |
| Phased roadmap | [planning/messaging-topology-and-sessions/Feature-007-Roadmap.md](../../../planning/messaging-topology-and-sessions/Feature-007-Roadmap.md) | Phases 0–6; per-phase effort and exit gates |
| Sessions/partitions design | [planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md](../../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) | `SendContext`, per-provider mapping |
| Topology-builder design | [planning/messaging-topology-and-sessions/Topology-Builder-Design.md](../../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md) | `ITopologyBuilder`, per-provider SDK mapping |
| Provider enhancement matrix | [planning/messaging-topology-and-sessions/Provider-Enhancement-Matrix.md](../../../planning/messaging-topology-and-sessions/Provider-Enhancement-Matrix.md) | Gap table + effort per cell |
| Advantages / rollout | [planning/messaging-topology-and-sessions/Advantages.md](../../../planning/messaging-topology-and-sessions/Advantages.md) | Outcomes and rollout recommendation |

The planning folder holds **inputs**, not a parallel spec. This file is the only authoritative feature spec.

---

## Problem Statement

Every broker in the rig has a "messages with the same key go to the same consumer, in order" primitive, but the rig treats them as opaque today. `OrderingAssert.PerKeyMonotonic` ships in the base package but no sender lets you set the key; Kafka even conflates it with `correlationId` at [`KafkaEventSender.cs:34`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34). Topology creation is equally inconsistent — ServiceBus needs a static JSON seed, RabbitMQ only declares queues, SQS and NATS have nothing at all.

**Mission**: introduce `SendContext` + `ITopologyBuilder` in the base library and roll them out to all 5 providers under strict RED → GREEN TDD, with coverage gates, provider-parity architecture tests, and in-PR documentation.

---

## User Stories

### User Story 1 — Per-key ordered delivery across every provider (Priority: P1)

As an application developer using the rig, I need to set one key (`SessionKey` / `PartitionKey`) on a sender and have the message routed through the provider's native ordering primitive, so `OrderingAssert.PerKeyMonotonic` passes end-to-end.

**Acceptance Scenarios**:
1. **Given** a ServiceBus sender and a topic with session-required subscription, **When** I send 100 messages across 10 `SessionKey`s via the `SendContext` overload, **Then** `ServiceBusSessionListener` records them with `CapturedMessage.SessionKey` set and `OrderingAssert.PerKeyMonotonic` passes.
2. **Given** a Kafka sender against a 6-partition topic, **When** I send 5 × 20 messages per `PartitionKey`, **Then** each key lands on a single partition and `OrderingAssert` passes per key.
3. **Given** an SQS sender against a FIFO queue, **When** I send messages across 5 `MessageGroupId`s (`SendContext.SessionKey`), **Then** ordering within each group is preserved and the dedup window is respected via `IsolationKey`-prefixed `DeduplicationKey`.
4. **Given** a RabbitMQ sender, **When** I send with explicit `exchange` + `routingKey` and a topic-exchange binding, **Then** only queues matching the binding receive the message and `CapturedMessage.SessionKey` reflects the `x-partition-key` header.
5. **Given** a NATS JetStream ordered consumer, **When** the connection briefly drops mid-stream, **Then** the consumer resumes without duplicates and `OrderingAssert` across the global sequence still passes.

### User Story 2 — Runtime topology creation on every provider (Priority: P1)

As an application developer, I need a fluent `WithTopology(t => …)` hook on every `{Provider}RigBuilder` that creates topics, subscriptions, exchanges, bindings, queues (including FIFO and DLQ) and streams at test setup, so I don't need external seed files or container pre-state.

**Acceptance Scenarios**:
1. **Given** a `ServiceBusRigBuilder`, **When** I call `.WithTopology(t => t.Topic("orders").Subscription("orders", "shipping", s => s.WithRequiresSession()).Queue("orders-dlq"))`, **Then** `BuildAsync` creates the entities via `ServiceBusAdministrationClient`; re-running is idempotent.
2. **Given** a `KafkaRigBuilder`, **When** I declare `.WithTopic("orders", partitions: 6, configs: {"cleanup.policy": "compact"})`, **Then** the topic exists with those configs after `BuildAsync`.
3. **Given** a `RabbitMqRigBuilder`, **When** I declare exchange + queue + binding + DLX, **Then** the topology is visible to the broker and a nack routes the message to the DLX.
4. **Given** a `SqsRigBuilder`, **When** I declare `.WithFifoQueue("orders", contentBasedDeduplication: true)`, **Then** a `.fifo` queue with the attribute is created.
5. **Given** a `NatsRigBuilder`, **When** I declare `.WithStream + .WithConsumer`, **Then** JetStream resources exist and the ordered consumer delivers messages.

### User Story 3 — Provider parity is enforced by architecture test (Priority: P2)

As a library maintainer, I need the provider-completeness architecture test to fail whenever any in-scope provider is missing `WithTopology`, a `SendContext` sender overload, or (where native) a session-aware listener, so no phase can silently regress shape.

**Acceptance Scenarios**:
1. **Given** Phase 0 has landed but Phase 1 has not, **When** CI runs, **Then** `ProviderCompletenessTests` is RED and the PR description lists the expected gap.
2. **Given** a future PR drops `WithTopology` from any `{Provider}RigBuilder`, **When** CI runs, **Then** the parity test fails and merge is blocked.

### User Story 4 — Coverage gate and docs stay green in-PR (Priority: P2)

As a CI pipeline operator, I need every PR in this feature to keep affected packages at ≥ 90 % line / ≥ 85 % branch coverage **and** ship documentation updates inline with the code.

**Acceptance Scenarios**:
1. **Given** any Feature 007 PR is opened, **When** the Codecov diff is computed, **Then** no touched package falls below the gate.
2. **Given** a PR adds a new public type, **When** the reviewer inspects coverage, **Then** that type is 100 % line-covered in the same PR.
3. **Given** a PR changes public API, **When** the reviewer inspects the diff, **Then** the relevant `README.md` / `docs/providers/*.md` / `CHANGELOG.md` entries are included.

### User Story 5 — Rollout ships in three separable minor releases (Priority: P3)

As a release manager, I need Feature 007 to land in three minor releases (Phase 0+1+2+3 first, Phase 4 next, Phase 5 deferred) with independent CHANGELOG entries, so NATS JetStream doesn't block the user's primary ServiceBus / Kafka / SQS ask.

**Acceptance Scenarios**:
1. **Given** Phase 0–3 + Phase 6 subset are merged, **When** the minor release ships, **Then** the CHANGELOG has one entry per shipped phase.
2. **Given** Phase 4 is ready two weeks later, **When** it ships, **Then** a new CHANGELOG entry is added without batching.

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance criterion | Validation method |
|----|-------------|----------------------|-------------------|
| **FR-007-01** | Every provider sender exposes the provider's native session / partition / group key through the common `SendContext` record. | Each of `ServiceBusEventSender`, `KafkaEventSender`, `RabbitMqEventSender`, `NatsJetStreamEventSender`, `SqsEventSender` has a `SendAsync` overload accepting `SendContext` that propagates `SessionKey` / `PartitionKey` / `DeduplicationKey` to the correct SDK field. | Integration test per provider sends N messages across M keys and asserts `OrderingAssert.PerKeyMonotonic` passes. Unit test per provider asserts the SDK field was populated (mock client). |
| **FR-007-02** | Every in-scope provider `RigBuilder` exposes a fluent `WithTopology(Action<ITopologyBuilder>)` hook that materialises declarations via the provider's admin / channel / management SDK. | All five `RigBuilder`s compile a call to `.WithTopology(t => ...)` and, on `BuildAsync`, apply every declaration idempotently. | Unit test per provider mocks the admin SDK and asserts the expected `CreateXxxAsync` was called with the expected options. Integration test per provider declares non-trivial topology against the real container. |
| **FR-007-03** | `OrderingAssert.PerKeyMonotonic` validates per-key ordering end-to-end on all 5 providers. | 5 integration tests (one per provider) publish interleaved per-key streams and call `OrderingAssert.PerKeyMonotonic(listener, m => m.SessionKey, m => m.Sequence)` — all green on CI. | CI matrix run must show 5 green messaging-integration jobs. `PerKeyMonotonic` public signature unchanged ([OrderingAssert.cs](../../../src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs)). |
| **FR-007-04** | No regression in existing integration tests on any provider. | Every previously-green integration and unit test stays green — delta from `master` baseline = 0 failures / 0 skipped. | CI gate: `master`-vs-branch run comparison; coverage baseline comparison. |
| **FR-007-05** | Coverage stays ≥ 90 % line / ≥ 85 % branch per affected source package. | Every package that Feature 007 modifies or creates reports ≥ 90 % line and ≥ 85 % branch in `coverage-scan-results/summary.csv`. | Codecov / coverage-scan workflow gate; PR must not lower any package's gate status. |
| **FR-007-06** | ServiceBus administration client works against the local emulator image in use (≥ 1.1.2). | `ServiceBusAdministrationHelper` creates topics, subscriptions (incl. `RequiresSession`, DLQ, SQL filter) with no 501/NotImplemented path. Any emulator-unsupported operation is documented and falls back to the JSON seed. | Phase 1 exit-gate integration tests (T015a–d) pass on Linux and Windows CI. Gap log in `docs/providers/service-bus.md` if any fallback is required. |
| **FR-007-07** | `NATS.Client.JetStream` is referenced only by `Rig.TUnit.Messaging.Nats`; the base library stays dependency-clean. | `Directory.Packages.props` has `NATS.Client.JetStream` ≥ 2.x; referenced by `Rig.TUnit.Messaging.Nats.csproj` only. | Architecture test (extend `DependencyDirectionTests`) asserts no other project references the JetStream package. |
| ~~**FR-007-08**~~ | ~~Public API is additive only; every new parameter optional.~~ | **Superseded by C-000** — packages are pre-release; breaking changes are permitted when they yield a cleaner surface. Retained as an inline note only. | n/a — removed from gate. |

### Non-Functional Requirements

- **NFR-C1** — coverage gate ≥ 90 % line / ≥ 85 % branch per package touched. New public types 100 % line-covered in their introducing PR.
- ~~**NFR-C2**~~ — **superseded by C-000**. Packages are pre-release.
- **NFR-C3** — docs (top-level `README.md`, **per-package family + provider `src/Rig.TUnit.Messaging*/README.md`**, `docs/providers/*.md`, `docs/glossary.md`, `CHANGELOG.md`, `docs/ordering-assertions.md`, inline XML on every new public member) ship in the **same PR** as the public-API change. Per-package READMEs must extend the existing 14-section canonical structure (enforced by `ReadmeCompletenessTests`) — no new H2 headings required, but each provider's README MUST mention every Feature 007 type it ships in its `Fixture + helper APIs`, `Quick start`, and `Provider quirks + edge cases` sections (review-gap finding from T060 → T064).
- **NFR-C4** — provider-parity architecture test green from Phase 0 exit onward (extends `ProviderCompletenessTests`, `DependencyDirectionTests`). Per C-003, parity asserts **presence** of a `WithTopology(Action<T>)` method on every `{Provider}RigBuilder` with `T : ITopologyBuilder`; the specific `T` intentionally varies because each provider's fluent surface is provider-scoped.
- **NFR-C5** — Phase 6 populates `benchmarks/baseline-007.json` (or appends to `baseline-006.json`) with ≥ 2 scenarios: ServiceBus session vs non-session; Kafka multi-partition per-key.

### Project-rule cross-references

- `.claude/rules/testing.md` — RED→GREEN naming `{Method}_{Scenario}_{ExpectedResult}`, Arrange-Act-Assert, no `Thread.Sleep`, no shared state.
- `.claude/rules/coding-style.md` — file-scoped namespaces, `sealed` on concrete types, `record` for `SendContext`, nullable reference types enabled.
- `.claude/rules/multi-repo.md` — branch name `feat/007-messaging-topology-sessions`, no `--no-verify`, new commits not amends, no destructive git operations without user approval.
- `.claude/rules/api-design.md` — HTTP-facing; not applicable to messaging surface; no conflict.

---

## Key Entities

- **`SendContext`** (new record) — `SessionKey`, `PartitionKey`, `DeduplicationKey`.
- **`ITopologyBuilder`** (new, base library) — **marker interface only**, carries `ApplyAsync(CancellationToken)`. Provider-specific fluent surface lives on provider-specific sub-interfaces (resolved C-003).
- **Provider-specific topology-builder interfaces** — `IServiceBusTopologyBuilder`, `IKafkaTopologyBuilder`, `IRabbitMqTopologyBuilder`, `INatsTopologyBuilder`, `ISqsTopologyBuilder` (each in the matching provider package). Only methods the provider genuinely supports are declared — unsupported concepts are **absent**, not no-ops or throws.
- **Provider-specific config interfaces** — `IServiceBusQueueConfig` / `IServiceBusTopicConfig` / `IServiceBusSubscriptionConfig`; `IKafkaTopicConfig`; `IRabbitMqExchangeConfig` / `IRabbitMqQueueConfig`; `INatsStreamConfig` / `INatsConsumerConfig`; `ISqsQueueConfig`. Each owns only methods its broker understands.
- **Per-provider topology-builder implementations** — `ServiceBusTopologyBuilder`, `KafkaTopologyBuilder`, `RabbitMqTopologyBuilder`, `NatsTopologyBuilder`, `SqsTopologyBuilder` (**sealed public**; implement their provider-specific interface).
- **Per-provider senders (new overloads)** — `{Provider}EventSender.SendAsync(string, SendContext, …)` on all five providers.
- **New listener types** — `ServiceBusSessionListener`, `NatsJetStreamListener` (Kafka / SQS / RabbitMQ extend existing listeners).
- **New fixture** — `NatsJetStreamFixture` (core `NatsFixture` untouched).
- **Extended `CapturedMessage<TMessage>`** — per C-001: keeps `TMessage Message`, tightens `string? Body` → `string Body`, adds trailing `string? SessionKey = null`.

---

## Architecture Scope

This feature is **generic mode, clean-architecture style** applied to the messaging sub-tree:

- **Base library** (`src/Rig.TUnit.Messaging`) — contracts: `SendContext`, `ITopologyBuilder`, config interfaces, `MessagingRigBuilder<TSelf>.WithTopology`, `CapturedMessage<TMessage>.SessionKey`.
- **Per-provider packages** (`src/Rig.TUnit.Messaging.{ServiceBus|Kafka|RabbitMq|Nats|Sqs}`) — new `Topology/` folder per package + sender/listener extensions.
- **Test projects** — unit tests under `.Tests.Unit`, integration tests under `.Tests.Integration`, architecture assertions in `Rig.TUnit.Architecture.Tests`.
- **Docs** — `README.md`, `docs/providers/*.md` (5 files, some new), `docs/ordering-assertions.md`, `CHANGELOG.md`, inline XML on every new public member.

Dependency direction remains: `Messaging.{Provider}` → `Messaging` → `Core`. No new inter-provider edges.

---

## Task List

Mirrors [Feature-007-Roadmap.md](../../../planning/messaging-topology-and-sessions/Feature-007-Roadmap.md) T000 → T063. Every task row declares: RED test file(s), one-line RED assertion, GREEN production file(s), docs touched, effort (🟢 < 4 h / 🟡 4–8 h / 🔴 > 8 h or new dep), dependencies, and commit pairing.

### Commit discipline

Every task that produces production code ships as **two commits**:

1. **RED commit** — test file only; asserts the intended behaviour; must fail the build or test. Prefix: `test(007): RED …`.
2. **GREEN commit** — minimum production change to make the test pass. No incidental refactor. Prefix: `feat(007): GREEN …` or `fix(007): GREEN …`.

Per **C-002**: every integration scenario (T015a–d, T025a–b, T033a–c, T044a–d, T055a–c) is also a discrete RED+GREEN pair. RED scenario tests land **at the start of their phase** before any provider production code; the subsequent unit-level GREEN commits are what flip each scenario RED → GREEN.

Single-GREEN is allowed only for: (a) version-bump tasks that add no production behaviour (T014, T050); (b) config-file shrinkage gated on pre-existing tests (T016); (c) docs-only tasks (T060, T061, T062, T063, T064).

---

### Phase 0 — Cross-cutting abstractions (base library)

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T000** | Introduce `SendContext` record; extend `EventSenderBase.BuildHeaders` to accept and propagate it. | `tests/Rig.TUnit.Messaging.Tests.Unit/Helpers/SendContextTests.cs` — asserts `SendContext` default is all-nulls; asserts `BuildHeaders(ctx)` is behaviourally identical to the old overload when `ctx` is default. | `src/Rig.TUnit.Messaging/Helpers/SendContext.cs` (new) + `src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs`. | `README.md` messaging section (intro para); inline XML on `SendContext` and the new `BuildHeaders` overload. | 🟢 | — | RED + GREEN |
| **T001** | Define the base-library `ITopologyBuilder` **marker interface only** (`ApplyAsync(CancellationToken)`). **No** fluent / config methods here — per C-003 those live on provider-specific sub-interfaces inside each provider package. | `tests/Rig.TUnit.Messaging.Tests.Unit/Topology/ITopologyBuilderContractTests.cs` — asserts the marker exposes `ApplyAsync`; asserts it has no other methods (regression guard against future unification attempts). | `src/Rig.TUnit.Messaging/Topology/ITopologyBuilder.cs` (new — marker only). | Inline XML on `ITopologyBuilder`; `docs/ordering-assertions.md` (create) — note that provider-specific topology builders extend this marker. | 🟢 | T000 | RED + GREEN |
| **T002** | Regression guard — `MessagingRigBuilder<TSelf>` base class must not declare `WithTopology` (per C-003). Each provider's `RigBuilder` declares its own strongly-typed `WithTopology(Action<I{Provider}TopologyBuilder>)` in its own phase (Phase 1 T013, Phase 2 T023, Phase 3 T031, Phase 4 T042, Phase 5 T054). | `tests/Rig.TUnit.Messaging.Tests.Unit/Builder/MessagingRigBuilderNoGenericWithTopologyTests.cs` — asserts base class declares **no** `WithTopology` method (prevents future re-unification). | `src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs` — XML doc comment block citing C-003 (no signature change). | `README.md` — note under "How topology works" that `WithTopology` is provider-specific. | 🟢 | T001 | **single GREEN** (structural assertion — test passes from day one, no RED state to drive; see analysis.md). |
| **T003** | Extend `ProviderCompletenessTests` + `DependencyDirectionTests` with parity assertions (NFR-C4). Per C-003, parity asserts *presence* of a `WithTopology(Action<T>)` method on each `{Provider}RigBuilder` where `T : ITopologyBuilder` — the specific `T` is provider-scoped and intentionally varies. Per C-005, the test reads its provider coverage list from `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (one provider assembly name per line). Phase 0 lands the file **empty** (test passes vacuously); each provider phase's GREEN commit appends its assembly name to the file. | `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` (extend) — asserts, for every assembly in `.parity-coverage.txt`, that its `RigBuilder` declares a `WithTopology(Action<TBuilder>)` method where `TBuilder : ITopologyBuilder`; that its sender declares a `SendContext` overload; and that session-capable providers declare a session-aware listener. Separate assertion: `.parity-coverage.txt` exists and every entry corresponds to a loadable assembly. | `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (new, empty) + test extension. | `README.md` — note provider-parity coverage and the progressive-enforcement file under "How testing works". | 🟢 | T002 | RED + GREEN (Phase 0 lands the test + empty file in the GREEN commit; test passes vacuously on `master`, no RED-on-master window). Every provider phase's GREEN commit adds one line to `.parity-coverage.txt` — the diff is the rollout progress signal. |

**Phase 0 exit gate**: base package compiles; T000–T002 green; T003 green with empty `.parity-coverage.txt` (test passes vacuously — no RED-on-master window per C-005). No provider regressions. Base library coverage ≥ 90/≥ 85.

---

### Phase 1 — Azure Service Bus

**Phase 1 commit order (outside-in)**: T015a RED → T015b RED → T015c RED → T015d RED → T014 (version bump + probe) → T010 RED → T010 GREEN → T011 RED → T011 GREEN (flips T015a to GREEN) → T012 RED → T012 GREEN (flips T015b/c/d to GREEN) → T013 RED → T013 GREEN → T016 GREEN.

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T015a** | Scenario: session FIFO ordering. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/SessionFifoOrderingTests.cs` — 100 messages across 10 `SessionKey`s; `OrderingAssert.PerKeyMonotonic` passes. | GREEN closed by T010 + T011. | `docs/providers/service-bus.md` | 🟢 | — (leads Phase 1) | **RED** first; **GREEN** = T010 + T011. |
| **T015b** | Scenario: partitioned topic fan-out. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/PartitionedFanoutTests.cs` — messages with distinct `PartitionKey`s reach every partition-aware receiver. | GREEN closed by T010 + T012. | `docs/providers/service-bus.md` | 🟢 | — (leads Phase 1) | **RED** first; **GREEN** = T010 + T012. |
| **T015c** | Scenario: DLQ on max delivery count. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/DlqRedriveTests.cs` — message repeatedly abandoned; `DeadLetterAssert` sees it after `MaxDeliveryCount`. | GREEN closed by T012. | `docs/providers/service-bus.md` | 🟢 | — (leads Phase 1) | **RED** first; **GREEN** = T012. |
| **T015d** | Scenario: SQL filter subscription. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/SqlFilterTests.cs` — subscription with `SqlRuleFilter("Region='EU'")` receives only EU-tagged messages. | GREEN closed by T012. | `docs/providers/service-bus.md` | 🟢 | — (leads Phase 1) | **RED** first; **GREEN** = T012. |
| **T014** | Bump `Azure.Messaging.ServiceBus` to ≥ 7.20.1. Capability probe runs before any admin-client consumer. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Topology/ServiceBusEmulatorCapabilityProbeTests.cs` — enumerates admin-client operations against the emulator; flags unsupported ops via `Assert.Inconclusive` with task-ID cross-ref. | `Directory.Packages.props` | `docs/providers/service-bus.md` — emulator capability table. | 🟢 | T015a–d | single GREEN (version bump + informational probe). |
| **T010** | Add `SendContext` overload to `ServiceBusEventSender.SendAsync`; map `SessionKey` → `ServiceBusMessage.SessionId`, `PartitionKey` → `.PartitionKey`; enforce `PartitionKey == SessionKey` when both set; `DeduplicationKey` → `MessageId`. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/ServiceBusEventSenderSendContextTests.cs` — asserts `SessionId` populated from `SessionKey`; asserts `InvalidOperationException` when both keys set unequally. | `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs` | `docs/providers/service-bus.md` — session usage section; inline XML. | 🟡 | T000, T014 | RED + GREEN |
| **T011** | Add `ServiceBusSessionListener` using `ServiceBusClient.CreateSessionProcessor`; populate `CapturedMessage<TMessage>.SessionKey`. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/ServiceBusSessionListenerTests.cs` — 10 sessions × 10 messages; listener records with `SessionKey`; `OrderingAssert.PerKeyMonotonic` passes. | `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs` (new) | `docs/providers/service-bus.md` — session listener section; inline XML. | 🟡 | T010 | RED + GREEN |
| **T012** | Define `IServiceBusTopologyBuilder`, `IServiceBusTopicConfig`, `IServiceBusSubscriptionConfig`, `IServiceBusQueueConfig` (provider-scoped, per C-003) and their sealed impls + `ServiceBusAdministrationHelper` wrapping `ServiceBusAdministrationClient` — `CreateTopicAsync`, `CreateSubscriptionAsync` (`RequiresSession`, `DefaultMessageTimeToLive`, `DeadLetteringOnMessageExpiration`, `LockDuration`, `MaxDeliveryCount`, `SqlRuleFilter`). | Unit: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/Topology/ServiceBusAdministrationHelperTests.cs` — mock client, assert `CreateTopicAsync(options)` with exact options; `IServiceBusQueueConfig.WithRequiresSession(true)` produces `RequiresSession=true` in the emitted options. Integration: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Topology/ServiceBusAdminEmulatorTests.cs` — topic + subscription + DLQ + SQL filter + idempotency. | `src/Rig.TUnit.Messaging.ServiceBus/Topology/IServiceBusTopologyBuilder.cs`, `IServiceBusTopicConfig.cs`, `IServiceBusSubscriptionConfig.cs`, `IServiceBusQueueConfig.cs`, `ServiceBusTopologyBuilder.cs`, `ServiceBusAdministrationHelper.cs` (all new in the ServiceBus package). | `docs/providers/service-bus.md` — admin-client section, migration note; inline XML on every new interface member. | 🔴 | T001, T011 | RED + GREEN |
| **T013** | Declare `ServiceBusRigBuilder.WithTopology(Action<IServiceBusTopologyBuilder> configure)` (provider-scoped — per C-003) and wire it to the admin helper. | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Topology/ServiceBusRigBuilderWithTopologyTests.cs` — build rig with `.WithTopology(t => t.Topic("…").Subscription("…", "…", s => s.WithRequiresSession()))`; post-`BuildAsync` topology exists. Compile-time: a separate `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/Topology/ServiceBusBuilderCompileFenceTests.cs` with an intentionally-failing `#if COMPILE_FENCE` block asserting that `.WithFifo()` does not exist on `IServiceBusQueueConfig`. | `src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilder.cs` | `README.md` — ServiceBus `WithTopology` example; `docs/providers/service-bus.md` updated. | 🟢 | T012 | RED + GREEN |
| **T016** | Shrink `service-bus-config.json` to namespace only. | — (regression coverage is the existing suite). | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/TestInfrastructure/service-bus-config.json` | `docs/providers/service-bus.md` — migration note. | 🟢 | T012, T013, T015a–d | single GREEN (config shrink). |

**Phase 1 exit gate**: existing ServiceBus tests stay green; T015a–d all flipped GREEN by end of T013; `Rig.TUnit.Messaging.ServiceBus` coverage ≥ 90/≥ 85; T013 GREEN commit appends `Rig.TUnit.Messaging.ServiceBus` to `.parity-coverage.txt` (parity test now enforces it).

---

### Phase 2 — Kafka

**Phase 2 commit order**: T025a RED → T025b RED → T020 RED → T020 GREEN → T021 RED → T021 GREEN → T022 RED → T022 GREEN (flips T025a GREEN) → T023 RED → T023 GREEN (flips T025b GREEN) → T024 RED → T024 GREEN.

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T025a** | Scenario: multi-partition per-key ordering via `OrderingAssert.PerKeyMonotonic`. | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/MultiPartitionOrderingTests.cs` — 6-partition topic, 5 keys × 20 messages, per-key monotonic. | GREEN closed by T020 + T021 + T022. | `docs/providers/kafka.md` | 🟢 | — (leads Phase 2) | **RED** first; **GREEN** at end of T022. |
| **T025b** | Scenario: compacted-topic retention. | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/CompactedRetentionTests.cs` — `cleanup.policy=compact`; duplicate keys; older values compacted. | GREEN closed by T023. | `docs/providers/kafka.md` | 🟢 | — (leads Phase 2) | **RED** first; **GREEN** = T023. |
| **T020** | Add `SendContext` overload to `KafkaEventSender.SendAsync`; `Message.Key = ctx.PartitionKey ?? ctx.SessionKey ?? correlationId ?? Guid`. Fixes the current correlation-id / partition-key conflation at [`KafkaEventSender.cs:34`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34). | `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/KafkaEventSenderSendContextTests.cs` — asserts `Message.Key` prefers `PartitionKey`; `correlationId` does not silently set the key when `PartitionKey` supplied. | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs` | `docs/providers/kafka.md` — explicit `PartitionKey` section. Inline XML. | 🟢 | T000 | RED + GREEN |
| **T021** | Add `KafkaFixtureOptions.DefaultPartitions` (default 1). | `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/KafkaFixtureOptionsTests.cs` — asserts default 1; `Range(1, 200)` validation. | `src/Rig.TUnit.Messaging.Kafka/Options/KafkaFixtureOptions.cs` | `docs/providers/kafka.md` — options table. | 🟢 | T020 | RED + GREEN |
| **T022** | Extend `KafkaListener.EnsureTopicExistsAsync` to honour partitions + configs (`retention.ms`, `cleanup.policy`, `min.insync.replicas`). | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/KafkaTopicConfigTests.cs` — 6-partition compacted topic; listener recovers configs via `AdminClient.DescribeConfigsAsync`. | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` | `docs/providers/kafka.md` — topic-config plumbing. | 🟢 | T021 | RED + GREEN |
| **T023** | Define `IKafkaTopologyBuilder` + `IKafkaTopicConfig` (provider-scoped, per C-003 — Kafka's surface is `Topic` only; `.Queue()` / `.Exchange()` simply **do not exist** — compile error to call). Implement `KafkaTopologyBuilder` wired to `AdminClient.CreateTopicsAsync`; expose `.WithPartitions`, `.WithReplicationFactor`, `.WithConfig(key, value)` on the topic config. Declare `KafkaRigBuilder.WithTopology(Action<IKafkaTopologyBuilder>)`. | Unit: `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/Topology/KafkaTopologyBuilderTests.cs` — mock `AdminClient`, assert `CreateTopicsAsync` called with expected `TopicSpecification`. Compile fence: `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/Topology/KafkaBuilderCompileFenceTests.cs` asserting `IKafkaTopologyBuilder` does not declare `.Queue` / `.Exchange` / `.Subscription` via reflection. Integration: `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Topology/KafkaTopologyBuilderLiveTests.cs`. | `src/Rig.TUnit.Messaging.Kafka/Topology/IKafkaTopologyBuilder.cs`, `IKafkaTopicConfig.cs`, `KafkaTopologyBuilder.cs` (new) + `KafkaRigBuilder.WithTopology` declaration. | `docs/providers/kafka.md` + `README.md` Kafka snippet. | 🟡 | T022 | RED + GREEN |
| **T024** | Optional helper: manual partition assignment for pinned-partition tests. | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/KafkaPinnedPartitionTests.cs` — consumer assigned to partition 3; only hash(3)-keyed messages delivered. | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` | `docs/providers/kafka.md` — pinned-partition note. | 🟢 | T023 | RED + GREEN |

**Phase 2 exit gate**: T025a & T025b both GREEN by end of T023; single-partition tests unchanged; `Rig.TUnit.Messaging.Kafka` coverage ≥ 90/≥ 85; T023 GREEN commit appends `Rig.TUnit.Messaging.Kafka` to `.parity-coverage.txt`.

---

### Phase 3 — SQS FIFO

**Phase 3 commit order**: T033a RED → T033b RED → T033c RED → T030 RED → T030 GREEN (flips T033a partial) → T031 RED → T031 GREEN (flips T033a/b/c) → T032 RED → T032 GREEN.

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T033a** | Scenario: FIFO ordering per group. | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/FifoOrderingTests.cs` — 5 groups × 10 messages; `OrderingAssert.PerKeyMonotonic` per group. | GREEN closed by T030 + T031. | `docs/providers/sqs.md` | 🟢 | — (leads Phase 3) | **RED** first; **GREEN** at end of T031. |
| **T033b** | Scenario: DLQ redrive. | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/DlqRedriveTests.cs` — message fails `MaxReceiveCount` times, arrives on DLQ. | GREEN closed by T031. | `docs/providers/sqs.md` | 🟢 | — (leads Phase 3) | **RED** first; **GREEN** = T031. |
| **T033c** | Scenario: content-based deduplication. | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/ContentBasedDedupTests.cs` — duplicate body within 5-min window sent once, received once. | GREEN closed by T031. | `docs/providers/sqs.md` | 🟢 | — (leads Phase 3) | **RED** first; **GREEN** = T031. |
| **T030** | Add `SendContext` overload to `SqsEventSender.SendAsync`; `SessionKey` → `MessageGroupId`; `DeduplicationKey` → `MessageDeduplicationId`. Throw `InvalidOperationException` on FIFO queue with null `SessionKey`. | `tests/Rig.TUnit.Messaging.Sqs.Tests.Unit/SqsEventSenderSendContextTests.cs` — asserts FIFO + missing `SessionKey` throws; asserts both IDs populated. | `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsEventSender.cs` | `docs/providers/sqs.md` — FIFO section; `IsolationKey` prefix guidance. Inline XML. | 🟢 | T000 | RED + GREEN |
| **T031** | Define `ISqsTopologyBuilder` + `ISqsQueueConfig` (provider-scoped, per C-003 — only `Queue` — no `.Topic()`, compile error to attempt SNS). `.WithFifo(bool contentBasedDeduplication = false)`, `.WithVisibilityTimeout`, `.WithDeadLetter(queue, maxReceiveCount)`, `.WithMessageRetentionPeriod` on `ISqsQueueConfig` mapped to `CreateQueueAsync` attributes. `.WithFifo()` appends `.fifo` suffix. Declare `SqsRigBuilder.WithTopology(Action<ISqsTopologyBuilder>)`. | Unit: `tests/Rig.TUnit.Messaging.Sqs.Tests.Unit/Topology/SqsTopologyBuilderTests.cs` — mock `IAmazonSQS`; assert `CreateQueueAsync` called with `FifoQueue=true`, `.fifo` suffix. Compile fence: `SqsBuilderCompileFenceTests.cs` asserts `ISqsTopologyBuilder` has no `.Topic` / `.Exchange` / `.Stream`. Integration: `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Topology/SqsTopologyBuilderLiveTests.cs`. | `src/Rig.TUnit.Messaging.Sqs/Topology/ISqsTopologyBuilder.cs`, `ISqsQueueConfig.cs`, `SqsTopologyBuilder.cs` (new) + `SqsRigBuilder.WithTopology` declaration. | `docs/providers/sqs.md` + `README.md` snippet. | 🟡 | T030 | RED + GREEN |
| **T032** | Listener requests `MessageGroupId` + `SequenceNumber` attributes; populates `CapturedMessage.SessionKey`. | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/SqsSessionListenerCaptureTests.cs` — send 3 messages with `MessageGroupId="g1"`; `CapturedMessage.SessionKey == "g1"`. | `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs` | `docs/providers/sqs.md` — listener behaviour. | 🟢 | T031 | RED + GREEN |

**Phase 3 exit gate**: T033a–c all GREEN by end of T031; existing standard-queue tests unchanged; `Rig.TUnit.Messaging.Sqs` coverage ≥ 90/≥ 85; T031 GREEN commit appends `Rig.TUnit.Messaging.Sqs` to `.parity-coverage.txt`.

---

### Phase 4 — RabbitMQ

**Phase 4 commit order**: T044a RED → T044b RED → T044c RED → T044d RED → T040 RED → T040 GREEN → T041 RED → T041 GREEN → T042 RED → T042 GREEN (flips T044a–d) → T043 RED → T043 GREEN.

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T044a** | Scenario: topic-exchange fan-out. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/TopicFanoutTests.cs` — 3 queues bound on `user.*`/`order.*`/`stock.*`; each receives only its subject. | GREEN closed by T040 + T041 + T042. | `docs/providers/rabbitmq.md` | 🟢 | — (leads Phase 4) | **RED** first; **GREEN** at end of T042. |
| **T044b** | Scenario: DLX on nack. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/DlxOnNackTests.cs` — nacked message routes via `x-dead-letter-exchange`. | GREEN closed by T042. | `docs/providers/rabbitmq.md` | 🟢 | — (leads Phase 4) | **RED** first; **GREEN** = T042. |
| **T044c** | Scenario: priority queue ordering. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/PriorityOrderingTests.cs` — priority queue delivers high-priority first. | GREEN closed by T042. | `docs/providers/rabbitmq.md` | 🟢 | — (leads Phase 4) | **RED** first; **GREEN** = T042. |
| **T044d** | Scenario: quorum queue. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/QuorumQueueTests.cs` — `x-queue-type=quorum` queue accepts messages and survives restart. | GREEN closed by T042. | `docs/providers/rabbitmq.md` | 🟢 | — (leads Phase 4) | **RED** first; **GREEN** = T042. |
| **T040** | Sender: add `SendContext` overload + explicit `exchange` + `routingKey`; default to existing behaviour when omitted. Writes `SendContext.PartitionKey` to `x-partition-key` header. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/RabbitMqEventSenderSendContextTests.cs` — `exchange` / `routingKey` passed to `BasicPublishAsync`; `x-partition-key` populated. | `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqEventSender.cs` | `docs/providers/rabbitmq.md` — routing-key + header conventions. Inline XML. | 🟢 | T000 | RED + GREEN |
| **T041** | Listener: declare exchange + binding before `BasicConsumeAsync`; pick up `x-partition-key` header into `CapturedMessage.SessionKey`. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/RabbitMqBindingListenerTests.cs` — topic exchange + queue bound on `orders.*`; send with `orders.eu`; `SessionKey` populated. | `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs` | `docs/providers/rabbitmq.md` — exchange + binding example. | 🟢 | T040 | RED + GREEN |
| **T042** | Define `IRabbitMqTopologyBuilder`, `IRabbitMqExchangeConfig`, `IRabbitMqQueueConfig` (provider-scoped, per C-003 — exposes `Exchange`, `Queue`, `Binding`; `.Subscription()` does not exist, compile error). Implement `RabbitMqTopologyBuilder` — `Exchange(name, type)`, `Queue(name, cfg)`, `Binding(exchange, queue, routingKey)` — wired to `ExchangeDeclareAsync` / `QueueDeclareAsync` / `QueueBindAsync`. `IRabbitMqQueueConfig` exposes `.WithMessageTtl`, `.WithMaxLength`, `.WithMaxPriority`, `.WithDeadLetterExchange`, `.WithQuorum`. Declare `RabbitMqRigBuilder.WithTopology(Action<IRabbitMqTopologyBuilder>)`. | Unit: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/Topology/RabbitMqTopologyBuilderTests.cs` — mock `IChannel`; assert `ExchangeDeclareAsync` / `QueueDeclareAsync` / `QueueBindAsync` with expected args. Compile fence: `RabbitMqBuilderCompileFenceTests.cs` asserts `IRabbitMqTopologyBuilder` has no `.Subscription` / `.Stream`; `IRabbitMqQueueConfig` has no `.WithFifo` / `.WithRequiresSession`. Integration: `RabbitMqTopologyLiveTests.cs`. | `src/Rig.TUnit.Messaging.RabbitMq/Topology/IRabbitMqTopologyBuilder.cs`, `IRabbitMqExchangeConfig.cs`, `IRabbitMqQueueConfig.cs`, `RabbitMqTopologyBuilder.cs` (new) + `RabbitMqRigBuilder.WithTopology` declaration. | `docs/providers/rabbitmq.md` — full DLX example; `README.md` Rabbit snippet. | 🔴 | T001, T041 | RED + GREEN |
| **T043** | Queue-argument plumbing: `x-dead-letter-exchange`, `x-dead-letter-routing-key`, `x-message-ttl`, `x-max-length`, `x-max-priority`, `x-queue-type=quorum`. | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/Topology/RabbitMqQueueArgsTests.cs` — every `With…` method produces the expected AMQP argument. | Same file as T042. | `docs/providers/rabbitmq.md` — queue-args reference table. | 🟢 | T042 | RED + GREEN |

**Phase 4 exit gate**: T044a–d all GREEN by end of T042; existing default-exchange tests unchanged; `Rig.TUnit.Messaging.RabbitMq` coverage ≥ 90/≥ 85; T042 GREEN commit appends `Rig.TUnit.Messaging.RabbitMq` to `.parity-coverage.txt`.

---

### Phase 5 — NATS JetStream

**Phase 5 commit order**: T055a RED → T055b RED → T055c RED → T050 GREEN (package ref + architecture guard) → T051 RED → T051 GREEN → T052 RED → T052 GREEN → T053 RED → T053 GREEN (flips T055a GREEN) → T054 RED → T054 GREEN (flips T055b+c GREEN).

Core-NATS fixture stays untouched.

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T055a** | Scenario: ordered delivery across reconnects. | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/OrderedReconnectTests.cs` — ordered consumer survives brief disconnect without duplicates. | GREEN closed by T053. | `docs/providers/nats.md` | 🟢 | — (leads Phase 5) | **RED** first; **GREEN** = T053. |
| **T055b** | Scenario: multi-subject filter. | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/MultiSubjectFilterTests.cs` — consumer with `FilterSubjects("a.*", "b.*")` only sees those subjects. | GREEN closed by T054. | `docs/providers/nats.md` | 🟢 | — (leads Phase 5) | **RED** first; **GREEN** = T054. |
| **T055c** | Scenario: retention policy. | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/RetentionPolicyTests.cs` — stream with `RetentionPolicy.Limits` + `MaxMsgs=10` drops oldest. | GREEN closed by T054. | `docs/providers/nats.md` | 🟢 | — (leads Phase 5) | **RED** first; **GREEN** = T054. |
| **T050** | Add `NATS.Client.JetStream` reference; extend `DependencyDirectionTests` to assert only the Nats project references it. | `tests/Rig.TUnit.Architecture.Tests/Rules/DependencyDirectionTests.cs` (extend) — asserts package referenced by Nats project only. | `Directory.Packages.props` + `src/Rig.TUnit.Messaging.Nats/Rig.TUnit.Messaging.Nats.csproj`. | `docs/providers/nats.md` (create) — dependency note. | 🟢 | — | single GREEN (package ref + guard; one GREEN ships the architecture assertion). |
| **T051** | `NatsJetStreamFixture` alongside `NatsFixture`. | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/NatsJetStreamFixtureTests.cs` — lifecycle, connection, JetStream context reachable. | `src/Rig.TUnit.Messaging.Nats/Fixtures/NatsJetStreamFixture.cs` (new) | `docs/providers/nats.md` — core vs JetStream split. Inline XML. | 🟡 | T050 | RED + GREEN |
| **T052** | `NatsJetStreamEventSender` using `INatsJSContext.PublishAsync`. `SessionKey` → subject suffix; `DeduplicationKey` → `Nats-Msg-Id` header. | Unit: `tests/Rig.TUnit.Messaging.Nats.Tests.Unit/NatsJetStreamEventSenderTests.cs` (mock `INatsJSContext`). Integration: `NatsJetStreamSenderLiveTests.cs`. | `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs` (new). | `docs/providers/nats.md` — JetStream send example. | 🟡 | T051 | RED + GREEN |
| **T053** | `NatsJetStreamListener` using ordered consumer (`DeliverPolicy.All`, `ReplayPolicy.Instant`, `FlowControl=true`). Populates `CapturedMessage.SessionKey` from subject segment. | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/NatsJetStreamListenerTests.cs` — ordered consumer records with `SessionKey`. | `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs` (new). | `docs/providers/nats.md` — ordered-consumer example. | 🟡 | T052 | RED + GREEN |
| **T054** | Define `INatsTopologyBuilder`, `INatsStreamConfig`, `INatsConsumerConfig` (provider-scoped, per C-003 — exposes `Stream`, `Consumer`; `.Queue()` / `.Topic()` / `.Exchange()` / `.Subscription()` do not exist, compile error). Implement `NatsTopologyBuilder` — `Stream(name, cfg)` + `Consumer(stream, name, cfg)` wired to `INatsJSContext.CreateStreamAsync` / `CreateConsumerAsync`. `INatsStreamConfig` exposes `.WithSubjects`, `.WithRetention(RetentionPolicy)`, `.WithMaxMessages`. Declare `NatsRigBuilder.WithTopology(Action<INatsTopologyBuilder>)`. | Unit: `NatsTopologyBuilderTests.cs` (mock `INatsJSContext`). Compile fence: `NatsBuilderCompileFenceTests.cs` asserts `INatsTopologyBuilder` has no `.Queue` / `.Topic` / `.Exchange`. Integration: `NatsTopologyBuilderLiveTests.cs`. | `src/Rig.TUnit.Messaging.Nats/Topology/INatsTopologyBuilder.cs`, `INatsStreamConfig.cs`, `INatsConsumerConfig.cs`, `NatsTopologyBuilder.cs` (new) + `NatsRigBuilder.WithTopology` declaration. | `docs/providers/nats.md` + `README.md` NATS snippet. | 🟡 | T001, T053 | RED + GREEN |

**Phase 5 exit gate**: core-NATS fixture untouched and green; JetStream suite green in its own CI matrix entry; T055a–c all GREEN by end of T054; `Rig.TUnit.Messaging.Nats` coverage ≥ 90/≥ 85; T054 GREEN commit appends `Rig.TUnit.Messaging.Nats` to `.parity-coverage.txt` (parity test row flips from empty to fully-enforcing).

---

### Phase 6 — Documentation & benchmarks

Per NFR-C3, per-public-API doc updates already landed inline with each provider phase. Phase 6 is the consolidation pass.

| Task | Description | RED test file — assertion | GREEN production file | Docs | Effort | Depends on | Commit pair |
|------|-------------|---------------------------|-----------------------|------|--------|------------|-------------|
| **T060** | Top-level README: add "Messaging topology & sessions" section with minimal per-provider example; update feature matrix / badges. | — (docs-only; covered by `ReadmeCompletenessTests`). | — | `README.md`; `docs/providers/*.md` cross-link audit. | 🟢 | all provider phases | single GREEN (docs-only). |
| **T061** | `CHANGELOG.md`: **one entry per shipped phase**, not batched. | — | — | `CHANGELOG.md` | 🟢 | T060 | single GREEN per phase entry (accumulated). |
| **T062** | Benchmarks: ServiceBus session vs non-session; Kafka multi-partition per-key. Land in existing `tests/Rig.TUnit.Benchmarks/` (see Q-4). | `tests/Rig.TUnit.Benchmarks/ServiceBusMessagingBenchmarks.cs` + `KafkaMessagingBenchmarks.cs` — new `[Benchmark]` methods; populate `benchmarks/baseline-007.json`. | Same files. | `docs/providers/service-bus.md`, `docs/providers/kafka.md` — benchmark reference. | 🟡 | all provider phases | single GREEN (benchmark additions). |
| **T063** | Update `OrderingAssert` XML docs with supported-providers matrix; mirror in `docs/ordering-assertions.md`. | — (docs-only). | `src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs` (XML only). | `docs/ordering-assertions.md` — capability matrix. | 🟢 | T060–T062 | single GREEN (docs-only). |
| **T064** | Per-package README sweep — extend the 14-section canonical READMEs of `src/Rig.TUnit.Messaging` (family base) and the five provider packages so they describe the Feature 007 surface (`SendContext`, session-aware listeners, `ITopologyBuilder` / `WithTopology`, admin helpers, queue/stream config interfaces). Top-level `README.md` adds a `WithTopology` capability matrix, a session-aware listener capability matrix, and an "administration helpers" sub-section. `docs/glossary.md` gets matching entries. **Reason**: post-merge review of T060 (commit `3db58f7`) caught that the docs-only task only touched the **top-level** README, leaving every provider package README still describing the pre-Feature-007 surface. | — (docs-only; covered by `ReadmeCompletenessTests` 14-section structural gate — extending existing sections, no new H2s). | — | 6 × `src/Rig.TUnit.Messaging.*/README.md`, top-level `README.md`, `docs/glossary.md`. | 🟢 | T063 | single GREEN (docs-only follow-up). |

**Phase 6 exit gate**: README clean (top-level **and** per-package family / provider READMEs reflect the shipped surface); `CHANGELOG.md` has one entry per shipped phase; `benchmarks/baseline-007.json` populated; `docs/ordering-assertions.md` lists all 5 providers.

---

## Coverage Plan

Baseline source: `coverage-scan-results/summary.csv` from Feature 006 scan run `24712477011`. Feature 006 exit gate guarantees every package row below is ≥ 90 / ≥ 85 before this branch is cut.

| Package | Pre-uplift line % (F006 input) | Post-F006 gate | Feature 007 new public types (must ship 100 % line) | Tests closing the gap | Expected post-F007 |
|---------|-------------------------------|-----------------|-------------------------------------------------------|------------------------|---------------------|
| `Rig.TUnit.Messaging` (base) | F006 exit gate ≥ 90 / ≥ 85 (pre-uplift was 30.9 % — Feature 006 closed the gap via T024) | ≥ 90 / ≥ 85 | `SendContext`, `ITopologyBuilder` (marker only — per C-003 no shared config interfaces), `CapturedMessage<TMessage>` (narrowed `Body` + new `SessionKey`), `EventSenderBase.BuildHeaders(SendContext, …)` overload | `SendContextTests`, `ITopologyBuilderContractTests`, `MessagingRigBuilderNoGenericWithTopologyTests` (Phase 0) | ≥ 90 / ≥ 85 |
| `Rig.TUnit.Messaging.ServiceBus` | 59.7 % (T033/F006) | ≥ 90 / ≥ 85 | `ServiceBusSessionListener`, `ServiceBusAdministrationHelper`, `ServiceBusTopologyBuilder`, `ServiceBusEventSender.SendAsync(SendContext,…)`, `ServiceBusRigBuilder.WithTopology(...)` | Phase 1: T010–T016 unit + integration suite | ≥ 90 / ≥ 85 |
| `Rig.TUnit.Messaging.Kafka` | ≥ 90 post-F006 | ≥ 90 / ≥ 85 | `KafkaTopologyBuilder`, `KafkaEventSender.SendAsync(SendContext,…)`, `KafkaFixtureOptions.DefaultPartitions`, `KafkaListener` partition-assignment helper, `KafkaRigBuilder.WithTopology(...)` | Phase 2: T020–T024 suite | ≥ 90 / ≥ 85 |
| `Rig.TUnit.Messaging.Sqs` | ≥ 90 post-F006 | ≥ 90 / ≥ 85 | `SqsTopologyBuilder`, `SqsEventSender.SendAsync(SendContext,…)`, `SqsListener` extensions, `SqsRigBuilder.WithTopology(...)` | Phase 3: T030–T032 suite | ≥ 90 / ≥ 85 |
| `Rig.TUnit.Messaging.RabbitMq` | ≥ 90 post-F006 | ≥ 90 / ≥ 85 | `RabbitMqTopologyBuilder`, `RabbitMqEventSender.SendAsync(SendContext,…)`, `RabbitMqListener` extensions, `RabbitMqRigBuilder.WithTopology(...)` | Phase 4: T040–T043 suite | ≥ 90 / ≥ 85 |
| `Rig.TUnit.Messaging.Nats` | ≥ 90 post-F006 | ≥ 90 / ≥ 85 | `NatsJetStreamFixture`, `NatsJetStreamEventSender`, `NatsJetStreamListener`, `NatsTopologyBuilder`, `NatsRigBuilder.WithTopology(...)` | Phase 5: T051–T054 suite | ≥ 90 / ≥ 85 |
| `Rig.TUnit.Architecture.Tests` (test project) | n/a | n/a | Parity assertions in `ProviderCompletenessTests` + JetStream guard in `DependencyDirectionTests` | T003 (RED), T050 | not gated |

**Reviewer rule**: Codecov diff must show every new public type at 100 % line coverage in its introducing PR. If not, merge blocked.

---

## Edge Cases

1. **ServiceBus: `SessionId` set with different `PartitionKey`** — sender throws `InvalidOperationException` pre-flight.
2. **Kafka: `correlationId` fallback** — existing tests relying on `Key = correlationId` keep working when the new overload is not used; chain is `PartitionKey ?? SessionKey ?? correlationId ?? Guid`.
3. **SQS FIFO dedup window (5 min)** — same `DeduplicationKey` across CI reruns drops messages silently; `IsolationKey` prefix is mandatory.
4. **Topology re-apply** — every `Create*Async` is idempotent (create-if-not-exists); re-running tests on a shared container must not fail.
5. **NATS JetStream reconnect mid-stream** — ordered consumer resumes without duplicates.
6. **Provider calls method it doesn't support** — **compile error** (C-003). Provider-scoped interfaces mean e.g. `IKafkaTopologyBuilder` has no `.Queue()` method at all, and `IRabbitMqQueueConfig` has no `.WithFifo()`. Dev gets IDE squiggle at keystroke time, not a runtime throw or silent no-op.
7. **Emulator capability gap** — T014 probes; any unsupported op falls back to JSON seed and is documented in `docs/providers/service-bus.md`.

---

## Risks & Mitigations

| # | Risk | Likelihood | Impact | Mitigation | Owner |
|---|------|------------|--------|------------|-------|
| R1 | ServiceBus emulator v1.1.2 admin-client operations incomplete | Medium | High | T014 capability probe runs **before** T010/T012. Unsupported ops fall back to JSON seed with a documented entry in `docs/providers/service-bus.md`. | Phase 1 owner |
| R2 | Kafka multi-partition rebalance timing flakes | Medium | Medium | Re-use the `partitionsAssigned` `TaskCompletionSource` pattern at [`KafkaListener.cs:66`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:66). | Phase 2 owner |
| R3 | JetStream container startup extends CI runtime | Medium | Low | Separate CI matrix entry; skip JetStream row when PR only touches core-NATS. | Phase 5 owner / DevOps |
| R4 | SQS FIFO 5-min dedup window causes cross-test interference | High | Medium | `IsolationKey`-prefixed `DeduplicationKey`. Documented in `docs/providers/sqs.md`. | Phase 3 owner |
| R5 | Public-API drift between providers | Medium | High | Parity test (T003, NFR-C4) enforces every assembly listed in `.parity-coverage.txt` (C-005). Removing an entry without user-visible justification blocks merge via PR review. | T003 owner |
| R6 | Single-GREEN leakage — config/version-bump tasks pull in production behaviour | Low | Medium | Every single-GREEN row reviewer-flagged; PR checklist confirms no production file outside declared GREEN targets is touched. | Reviewer |
| R7 | Roadmap path `benchmarks/Rig.TUnit.Messaging.Benchmarks/*` (T062) vs actual `tests/Rig.TUnit.Benchmarks/` | Certain | Low | Extend the existing shared project (Q-4). | Phase 6 owner |

---

## Rollout

Per [Advantages.md §5](../../../planning/messaging-topology-and-sessions/Advantages.md):

1. **Release N+1** — Phase 0 + 1 + 2 + 3 + Phase 6 subset. Covers ServiceBus sessions, Kafka decoupling, SQS FIFO. ~7 days serial.
2. **Release N+2** — Phase 4 (RabbitMQ). ~2 days.
3. **Release N+3** — Phase 5 (NATS JetStream). Additive-only new fixture. ~2.5 days.

Each minor release carries its own `CHANGELOG.md` entry and its own benchmark delta (NFR-C5).

---

## Out of Scope

Mirrored from the `## Non-goals` sections in [Feature-007-Roadmap.md](../../../planning/messaging-topology-and-sessions/Feature-007-Roadmap.md), [Sessions-And-Partitions-Design.md](../../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md), [Topology-Builder-Design.md](../../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md):

- **New messaging providers** (Pulsar, MQTT, Google Pub/Sub) — Feature 008+.
- **Redesign of `EventSenderBase` / `ListenerBase`** — only additive.
- **Removal of `service-bus-config.json`** — it stays as a namespace bootstrap.
- **Kafka exactly-once / transactions**.
- **RabbitMQ stream queues** (`x-queue-type=stream`) — later feature.
- **Cross-provider abstract `Session` type** — only the key parameter *name* is unified.
- **ServiceBus auto-renewing session locks** — tests set lock duration explicitly.
- **Infrastructure-as-code interpretation of `ITopologyBuilder`** — this is a *test-rig* feature; no Terraform / Bicep / Pulumi equivalence.
- **Migration tools** for `service-bus-config.json` — manual, one-time.
- **SNS integration on SQS topology builder** — `.Topic(...)` throws by design.
- **Changes to `OrderingAssert.PerKeyMonotonic` public signature** — only XML docs update in T063.

---

## Success Criteria

- **SC-007-01** — all 7 active FRs validated green on CI on the feature branch before merge.
- **SC-007-02** — coverage gate ≥ 90 / ≥ 85 on every affected package in `coverage-scan-results/summary.csv` post-merge.
- **SC-007-03** — architecture parity test (`ProviderCompletenessTests`) passes for every in-scope provider row.
- **SC-007-04** — `benchmarks/baseline-007.json` has ≥ 2 scenarios (ServiceBus session vs non-session; Kafka multi-partition per-key).
- **SC-007-05** — `CHANGELOG.md` has one entry per shipped phase (not batched).
- **SC-007-06** — every new public type / overload has inline XML docs and is referenced from `README.md` or `docs/providers/*.md`.

---

## Clarifications

Resolutions applied during `/dotnet-ai-kit:clarify` on 2026-04-23.

- **C-000** [Global] **Pre-release status**. Packages are unreleased; every clean-vs-compatible design fork resolves to *clean*. Applies to the whole repo until v1.0 ships. **NFR-C2 and FR-007-08 are superseded** and will be removed once `/dai.plan` opens.
- **C-001** [Domain & Data Model] **`CapturedMessage<TMessage>` shape** → keep `TMessage Message` (pairs with the broker-native received object, matches .NET convention). Tighten `string? Body` → `string Body` (listeners coerce `null` → `""` at capture). Add `string? SessionKey = null` trailing. Design doc [Sessions-And-Partitions-Design.md §Listener side](../../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) amended to match.
- **C-002** [Edge Cases / Commit Discipline] **Per-scenario RED+GREEN** for integration-scenario tests. Each of T015a–d, T025a–b, T033a–c, T044a–d, T055a–c is a discrete RED+GREEN pair. RED scenario tests land at the **start** of their phase, before any production code; subsequent unit-level GREEN commits are what flip each scenario RED → GREEN. Single-GREEN now restricted to: version-bump / architecture-guard tasks (T014, T050), config shrinkage gated on pre-existing tests (T016), docs-only tasks (T060–T063), and the Phase 0 regression guard T002.
- **C-003** [API shape] **Provider-scoped topology interfaces** — no shared `IQueueConfig` / `ITopicConfig` / etc. with every provider's methods stapled on. The base `ITopologyBuilder` is a **marker only** (`ApplyAsync(CancellationToken)`). Each provider package owns `I{Provider}TopologyBuilder` + provider-specific config interfaces (`IServiceBusQueueConfig`, `IKafkaTopicConfig`, `IRabbitMqQueueConfig`, `INatsStreamConfig`, `ISqsQueueConfig`, etc.). Unsupported operations **do not exist** on the target's surface — compile-time error, never a runtime throw, never a silent no-op. Each provider's `RigBuilder.WithTopology(Action<I{Provider}TopologyBuilder>)` receives its own strongly-typed builder. The parity test asserts *presence* of `WithTopology` (with any `T : ITopologyBuilder` parameter), not uniformity. Planning doc [Topology-Builder-Design.md §Public API](../../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md) amended. Phase 0 T001 shrinks to the marker; T002 reduces to a regression guard (base class declares no generic `WithTopology`); each provider's topology-builder task (T012, T023, T031, T042, T054) ships its provider-scoped interfaces + `RigBuilder` hook together, plus a compile-fence unit test asserting absence of unsupported methods.
- **C-004** [Edge Cases] **ServiceBus emulator gap handling**: when T014 capability probe reports an unsupported admin-client operation, the affected Phase 1 scenario test is annotated `[Skip("emulator-gap: <op> unsupported — see docs/providers/service-bus.md#emulator-gaps")]`, the gap is recorded in a named table in `docs/providers/service-bus.md`, and an upstream issue is filed referencing that table row. Phase 1 exit gate treats skipped scenarios as passing for parity / coverage purposes. No automated image-tag escalation (deferred). No JSON-seed fallback (would violate C-003's no-silent-fall-through spirit). Re-running the probe when the emulator image updates is a manual task on each image bump PR.
- **C-005** [Edge Cases / CI] **Progressive parity enforcement via `.parity-coverage.txt`**. T003 reads `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` — one provider assembly name per line — and enforces the parity contract only for listed providers. Phase 0 lands the file empty (test passes vacuously — **no RED-on-master window**). Each provider phase's GREEN commit appends its assembly name to the file; that diff is the visible rollout signal. No `[Skip]` annotations, no `continue-on-error`, no separate CI job. The parity test on `master` is always truthful about what has actually landed.

---

## Open Questions

All open questions resolved. Ready for `/dotnet-ai-kit:plan`.

| # | Topic | Status |
|---|-------|--------|
| ~~Q-3~~ | ~~Unsupported `IQueueConfig.With…` config-method policy~~ | **Resolved C-003** — provider-scoped interfaces eliminate the question. |
| ~~Q-4~~ | ~~ServiceBus emulator admin-client gap handling~~ | **Resolved C-004** — `[Skip]` + documented gap table + upstream issue. |
| ~~Q-5~~ | ~~T003 RED-only parity test CI strategy~~ | **Resolved C-005** — progressive enforcement via `.parity-coverage.txt`; no RED-on-master window. |

---

## Next

1. `/dotnet-ai-kit:plan` to produce `plan.md` from this spec + authoritative planning docs.
2. `/dotnet-ai-kit:tasks` to break T000–T063 into an ordered executable list.
