# Feature 007 — Messaging Topology & Sessions: Roadmap

**Status**: Planning
**Proposed branch**: `feat/007-messaging-topology-sessions`
**Depends on**: Feature 006 coverage gates green
**Providers in scope**: ServiceBus, Kafka, RabbitMQ, NATS, SQS (5 of 5 messaging families)

---

## Mission

Make every messaging provider in the rig support (a) per-key ordered delivery via the
provider's native session/partition/group primitive and (b) runtime topology creation via a
unified fluent `TopologyBuilder` — so integration tests can model realistic production
messaging patterns without external seed files, manual container prep, or provider-specific
ceremony.

## Non-goals

- New providers (Pulsar, MQTT, Google Pub/Sub) — those go in Feature 008+.
- Redesigning `EventSenderBase` / `ListenerBase` beyond additive changes.
- Removing the existing Service Bus JSON seed — it stays as a minimal bootstrap; the admin
  client layers on top.
- Breaking changes to any public API. All new parameters are optional with safe defaults.

## Delivery mode

RED → GREEN per task. Every new sender/listener parameter ships with an integration test that
proves per-key ordering via `OrderingAssert.PerKeyMonotonic`. Every topology-builder method
ships with a unit test (mock SDK) **and** an integration test against the real container.

---

## Phase 0 — Cross-cutting abstractions (base library)

**Goal**: Land the shared contracts before any provider work starts, so every subsequent
phase is a narrow, additive change to one provider.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T000 | Add `SessionKey` / `PartitionKey` optional parameter to `EventSenderBase.BuildHeaders` signature and propagate via a new `SendContext` record | `src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs` | 2 h |
| T001 | Define `ITopologyBuilder` interface in the base package (methods: `Topic`, `Subscription`, `Queue`, `Exchange`, `Binding`, `Stream` — each returning a fluent config object that providers bind to natively) | `src/Rig.TUnit.Messaging/Topology/ITopologyBuilder.cs` (new) | 3 h |
| T002 | Define `MessagingRigBuilder<TSelf>.WithTopology(Action<ITopologyBuilder>)` hook | `src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs` | 1 h |
| T003 | Unit tests for `ITopologyBuilder` default no-op and fluent chaining | `tests/Rig.TUnit.Messaging.Tests.Unit/Topology/*` | 2 h |

**Phase 0 exit gate**: Base package builds, all 4 tests green, no provider regressions.

**Effort total**: ~1 day.

---

## Phase 1 — Azure Service Bus (the lead provider)

**Goal**: Full sessions + admin-client topology; this is the primary user ask and the most
complex provider.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T010 | Add `sessionId` + `partitionKey` to `ServiceBusEventSender.SendAsync`; map to `ServiceBusMessage.SessionId` and `.PartitionKey`; enforce equality rule on session-aware entities | `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs` | 2 h |
| T011 | Add `ServiceBusSessionListener` using `CreateSessionProcessor`; record per-session ordered delivery | `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs` (new) | 4 h |
| T012 | Introduce `ServiceBusAdministrationHelper` wrapping `ServiceBusAdministrationClient` — `CreateTopicAsync`, `CreateSubscriptionAsync` (supports `RequiresSession`, `DefaultMessageTimeToLive`, `DeadLetteringOnMessageExpiration`, `LockDuration`, `MaxDeliveryCount`, `RuleProperties` / SQL filters) | `src/Rig.TUnit.Messaging.ServiceBus/Topology/` (new) | 6 h |
| T013 | Wire `ServiceBusRigBuilder.WithTopology(...)` to the admin helper | `src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilder.cs` | 2 h |
| T014 | Bump `Azure.Messaging.ServiceBus` to ≥ 7.20.1 | `Directory.Packages.props` | < 15 min |
| T015 | Integration tests: (a) session FIFO ordering, (b) partitioned topic fan-out, (c) DLQ on max delivery count, (d) SQL filter subscription | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/*` | 6 h |
| T016 | Update `service-bus-config.json` to minimal namespace only (let admin client do the rest) | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/TestInfrastructure/service-bus-config.json` | 1 h |

**Phase 1 exit gate**: All existing ServiceBus tests stay green. 4 new session + topology
integration tests green on Linux + Windows CI. Coverage of the package stays ≥ 90 % line.

**Effort total**: ~3 days.

**Risks**:
- Emulator v1.1.2 may not support every admin client operation — verify early in T014. If
  any op is unsupported, document and fall back to JSON for that specific feature.
- Session + partitioning interaction rule (PartitionKey == SessionId when both set) must be
  validated pre-emptively to surface a clean exception, not a brokered one.

---

## Phase 2 — Kafka (smallest change, biggest parity win)

**Goal**: Explicit partition key and configurable partitions unlock real per-key ordering
tests across partitions.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T020 | Add `partitionKey` parameter to `KafkaEventSender.SendAsync` distinct from `correlationId`; map to `Message.Key` | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs` | 1 h |
| T021 | `KafkaFixtureOptions.DefaultPartitions` (default 1 for back-compat, configurable) | `src/Rig.TUnit.Messaging.Kafka/Options/KafkaFixtureOptions.cs` | 1 h |
| T022 | Extend `EnsureTopicExistsAsync` to honour partitions + configs (retention.ms, cleanup.policy, min.insync.replicas) | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` | 2 h |
| T023 | `KafkaTopologyBuilder` — `WithTopic(name, partitions, replicationFactor, configs)` wired to `AdminClient.CreateTopicsAsync` | `src/Rig.TUnit.Messaging.Kafka/Topology/` (new) | 3 h |
| T024 | Optional: helper for manual partition assignment (`consumer.Assign(TopicPartition)`) for pinned-partition tests | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` | 2 h |
| T025 | Integration tests: multi-partition per-key ordering with `OrderingAssert.PerKeyMonotonic`; compacted topic retention test | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/*` | 4 h |

**Phase 2 exit gate**: Multi-partition ordering test green. Single-partition back-compat test
stays green without modification (default `Partitions = 1`).

**Effort total**: ~1.5 days.

---

## Phase 3 — SQS FIFO (self-contained, straightforward)

**Goal**: FIFO queue creation + `MessageGroupId` / `MessageDeduplicationId` plumbing.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T030 | Add `messageGroupId` + `messageDeduplicationId` to `SqsEventSender.SendAsync`; required when the queue is FIFO | `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsEventSender.cs` | 2 h |
| T031 | `SqsTopologyBuilder.WithFifoQueue(name, contentBasedDeduplication, attributes)` mapped to `CreateQueueAsync`; `WithStandardQueue(name, visibilityTimeout, redrivePolicy)` for DLQ tests | `src/Rig.TUnit.Messaging.Sqs/Topology/` (new) | 4 h |
| T032 | Listener: request `MessageGroupId` + `SequenceNumber` attributes so `OrderingAssert` can read them | `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs` | 1 h |
| T033 | Integration tests: FIFO ordering per group; DLQ redrive; content-based dedup | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/*` | 4 h |

**Phase 3 exit gate**: LocalStack FIFO test green. Standard queue tests unchanged.

**Effort total**: ~1.5 days.

---

## Phase 4 — RabbitMQ (largest new surface)

**Goal**: Full exchange/binding/DLX topology. Priority queues optional.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T040 | Sender: accept explicit `exchange` + `routingKey`; default to current "default exchange + queue name" behaviour when omitted | `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqEventSender.cs` | 2 h |
| T041 | Listener: support exchange + binding declaration before `BasicConsumeAsync` | `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs` | 2 h |
| T042 | `RabbitMqTopologyBuilder` — `WithExchange(name, type)`, `WithQueue(name, args)`, `WithBinding(exchange, queue, routingKey)`, `WithDeadLetterExchange(...)` mapped to `ExchangeDeclareAsync` / `QueueDeclareAsync` / `QueueBindAsync` | `src/Rig.TUnit.Messaging.RabbitMq/Topology/` (new) | 6 h |
| T043 | Support queue arguments: `x-dead-letter-exchange`, `x-dead-letter-routing-key`, `x-message-ttl`, `x-max-length`, `x-max-priority`, `x-queue-type=quorum` | same as T042 | 2 h |
| T044 | Integration tests: topic-exchange fan-out; DLX on nack; priority ordering; quorum queue | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/*` | 6 h |

**Phase 4 exit gate**: All 4 integration scenarios green. Existing default-exchange tests
unchanged.

**Effort total**: ~2 days.

---

## Phase 5 — NATS JetStream (largest investment)

**Goal**: Persistent, ordered delivery via JetStream. Core NATS pub/sub stays as is — this
is a **new fixture variant**, not a replacement.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T050 | Add `NATS.Client.JetStream` package reference | `Directory.Packages.props`, `Rig.TUnit.Messaging.Nats.csproj` | 15 min |
| T051 | New `NatsJetStreamFixture` alongside the existing `NatsFixture` | `src/Rig.TUnit.Messaging.Nats/Fixtures/NatsJetStreamFixture.cs` (new) | 3 h |
| T052 | `NatsJetStreamEventSender` using `INatsJSContext.PublishAsync` | `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs` (new) | 3 h |
| T053 | `NatsJetStreamListener` using ordered consumer (`ConsumerConfig` with ordered requirements) | `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs` (new) | 4 h |
| T054 | `NatsTopologyBuilder.WithStream(name, subjects, retention, maxMsgs)` + `WithConsumer(name, filterSubjects, deliverPolicy)` | `src/Rig.TUnit.Messaging.Nats/Topology/` (new) | 4 h |
| T055 | Integration tests: ordered delivery across reconnects; multi-subject filter; retention policy | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/*` | 6 h |

**Phase 5 exit gate**: Core NATS fixture unchanged and green. New JetStream fixture green in
CI. Two public fixtures documented side-by-side.

**Effort total**: ~2.5 days.

---

## Phase 6 — Documentation & benchmarks

**Goal**: Reflect new surface in every consumer-facing doc. Add benchmarks for session
dispatch overhead vs non-session.

| Task | Description | Files | Effort |
|------|-------------|-------|--------|
| T060 | Update README with topology-builder examples per provider | `README.md`, `docs/providers/*` | 3 h |
| T061 | Add CHANGELOG entry for Feature 007 | `CHANGELOG.md` | 30 min |
| T062 | Benchmark: ServiceBus session processor vs non-session; Kafka multi-partition per-key throughput | `benchmarks/Rig.TUnit.Messaging.Benchmarks/*` | 4 h |
| T063 | Update `OrderingAssert` XML docs with "supported providers" table | `src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs` | 1 h |

**Phase 6 exit gate**: README compiles; CHANGELOG merged; benchmark baseline populated.

**Effort total**: ~1 day.

---

## Total effort estimate

| Phase | Effort | Parallelisable |
|-------|--------|----------------|
| 0 — Cross-cutting abstractions | 1 d | no (blocks all) |
| 1 — Azure Service Bus | 3 d | no |
| 2 — Kafka | 1.5 d | yes (after Phase 0) |
| 3 — SQS FIFO | 1.5 d | yes (after Phase 0) |
| 4 — RabbitMQ | 2 d | yes (after Phase 0) |
| 5 — NATS JetStream | 2.5 d | yes (after Phase 0) |
| 6 — Docs & benchmarks | 1 d | no (runs last) |
| **Serial worst case** | **12.5 d** | |
| **Parallel best case (1 dev P1+5, 1 dev P2/3/4)** | **~7 d** | |

---

## Success criteria (FRs)

| ID | Criterion | Verification |
|----|-----------|--------------|
| FR-007-01 | Every provider sender supports the provider's native session/group key | Integration test per provider |
| FR-007-02 | Every provider has a `WithTopology` fluent hook on its `RigBuilder` | Unit test per provider |
| FR-007-03 | `OrderingAssert.PerKeyMonotonic` validates per-key ordering in end-to-end tests on all 5 providers | 5 integration tests green |
| FR-007-04 | No regression in existing integration tests for any provider | CI baseline comparison |
| FR-007-05 | Coverage stays ≥ 90 % line / ≥ 85 % branch per new package | Codecov gate |
| FR-007-06 | ServiceBus admin client works on emulator image 1.1.2 (or documented newer) | Phase 1 exit gate |
| FR-007-07 | `NATS.Client.JetStream` added only to the Nats package; base library stays dependency-clean | Architecture test |
| FR-007-08 | Public API is additive only; all new parameters are optional with safe defaults | API diff review |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| ServiceBus emulator admin-client operations incomplete in v1.1.2 | Medium | High | Probe in T014 before T012 implementation; fall back to JSON for unsupported ops and file an upstream issue |
| Kafka multi-partition rebalance timing flakes in CI | Medium | Medium | Reuse the partition-assigned TCS pattern from [KafkaListener.cs:66](../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:66) |
| JetStream container startup adds to CI runtime budget | Medium | Low | Run JetStream fixture in its own CI matrix entry; skip on PRs touching only core NATS |
| SQS FIFO deduplication window (5 min) causes cross-test interference | High | Medium | Use unique `MessageDeduplicationId` per test via `IsolationKey` prefix |
| Public API drift between providers if `ITopologyBuilder` methods diverge | Medium | High | Provider-parity architecture test enforces method presence; reuse existing provider-completeness test pattern |
