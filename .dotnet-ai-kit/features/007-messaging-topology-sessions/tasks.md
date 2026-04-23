# Tasks: Messaging Topology & Sessions

**Feature**: 007-messaging-topology-sessions | **Mode**: Generic (single-repo, messaging sub-tree)
**Generated**: 2026-04-23 | **Total tasks**: 72 (T002 collapsed to single-GREEN per analysis.md)
**Branch**: `feat/007-messaging-topology-sessions`
**Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md) · **Data model**: [data-model.md](data-model.md)

---

## Ordering rules

- **Phase 0 MUST merge first** before any provider phase begins.
- Phases 1, 2, 3, 4, 5 are parallel-eligible after Phase 0 (each can open its own sub-branch).
- **Within each provider phase**, the scenario RED tests lead: T015a–d, T025a–b, T033a–c, T044a–d, T055a–c are committed FIRST; provider implementation tasks' GREEN commits flip them to passing.
- Phase 6 is the final consolidation — docs + benchmarks — and may start as soon as the first provider phase reaches exit gate.

## Commit discipline

Every production-code task ships as **two commits**:
- `test(007): RED T0NN — <one-line assertion>` (test-only)
- `feat(007): GREEN T0NN — <summary>` or `fix(007): GREEN T0NN — <summary>` (production-only)

Single-GREEN tasks (version bumps, config shrinkage, docs-only) ship as one commit: `feat(007): GREEN T0NN — <summary>` with a trailing `(no red — <rationale>)` note.

No `--no-verify`, no amends across RED/GREEN boundary, no destructive git operations.

---

## Phase 0 — Cross-cutting base library (BLOCKING)

> One PR: `#007-p0`. Merge before any provider phase starts.

- [x] **T000-RED** Write `SendContextTests`.
      File: `tests/Rig.TUnit.Messaging.Tests.Unit/Helpers/SendContextTests.cs` (new)
      Tests:
      - `SendContext_Default_IsAllNulls`
      - `BuildHeaders_DefaultSendContext_ProducesSameHeadersAsLegacyOverload`
      - `BuildHeaders_WithSendContext_PreservesLegacyHeaderPropagation`
      Commit: `test(007): RED T000 — SendContext record shape + BuildHeaders overload parity`

- [x] **T000-GREEN** [depends: T000-RED] Add `SendContext` record + `BuildHeaders(SendContext, …)` overload; narrow `CapturedMessage<TMessage>.Body` from `string?` → `string`; add trailing `string? SessionKey = null`. **Ripples**: 3 provider listeners construct `CapturedMessage` with nullable body values today — coerce `null → string.Empty` at each call site so Phase 0 compiles at the solution level.
      Files:
      - `src/Rig.TUnit.Messaging/Helpers/SendContext.cs` (new)
      - `src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs` (overload added)
      - `src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs` (`CapturedMessage<TMessage>` record updated per C-001)
      - `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` line 151 — `result.Message.Value` → `result.Message.Value ?? string.Empty`
      - `src/Rig.TUnit.Messaging.Nats/Helpers/NatsListener.cs` line 84 — `msg.Data` → `msg.Data ?? string.Empty`
      - `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs` line 91 — `msg.Body` → `msg.Body ?? string.Empty`
      - (ServiceBus + RabbitMQ listeners already pass non-null — no change)
      - `README.md` — messaging section intro paragraph (mention `SendContext` + `SessionKey`)
      Commit: `feat(007): GREEN T000 — SendContext + BuildHeaders overload + CapturedMessage extension + listener null-coercion`

- [x] **T001-RED** [depends: T000-GREEN] Write `ITopologyBuilderContractTests`.
      File: `tests/Rig.TUnit.Messaging.Tests.Unit/Topology/ITopologyBuilderContractTests.cs` (new)
      Tests:
      - `ITopologyBuilder_DeclaresOnlyApplyAsync` — reflection asserts exactly one method (`ApplyAsync`).
      - `ITopologyBuilder_ApplyAsync_AcceptsCancellationToken`
      Commit: `test(007): RED T001 — ITopologyBuilder marker contract`

- [x] **T001-GREEN** [depends: T001-RED] Add `ITopologyBuilder` marker (no fluent methods — per C-003).
      Files:
      - `src/Rig.TUnit.Messaging/Topology/ITopologyBuilder.cs` (new)
      - `docs/ordering-assertions.md` (create — stub linking to per-provider docs; will be filled in T063)
      Commit: `feat(007): GREEN T001 — ITopologyBuilder marker interface`

- [x] **T002** [depends: T001-GREEN] Regression guard — `MessagingRigBuilder<TSelf>` base class must not declare `WithTopology` (per C-003). Single-GREEN: the test is structural, not behavioural, so there is no RED state to drive.
      Files:
      - `tests/Rig.TUnit.Messaging.Tests.Unit/Builder/MessagingRigBuilderNoGenericWithTopologyTests.cs` (new) — reflection asserts base class declares no `WithTopology` method.
      - `src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs` — add XML doc comment block noting `WithTopology` lives on provider-specific `RigBuilder`s per C-003.
      Commit: `feat(007): GREEN T002 — regression guard against base-class WithTopology (no red — structural assertion; test passes from day one)`

- [x] **T003-RED** [depends: T002] Write extended `ProviderCompletenessTests`.
      File: `tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs` (extend)
      New tests:
      - `Providers_InParityCoverage_DeclareWithTopology` — for every assembly in `.parity-coverage.txt`, its `RigBuilder` declares a `WithTopology(Action<T>)` method where `T : ITopologyBuilder`.
      - `Providers_InParityCoverage_DeclareSendContextOverload` — sender exposes a `SendAsync(string, SendContext, …)` overload.
      - `SessionCapableProviders_InParityCoverage_DeclareSessionListener` — ServiceBus / Kafka / NATS / SQS declare a session-aware or partition-aware `ListenerBase<>` subtype.
      - `ParityCoverageFile_Exists_WithLoadableAssemblies`
      Commit: `test(007): RED T003 — provider-parity driven by .parity-coverage.txt`

- [x] **T003-GREEN** [depends: T003-RED] Create empty `.parity-coverage.txt`; test passes vacuously per C-005.
      Files:
      - `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (new, empty)
      - `tests/Rig.TUnit.Architecture.Tests/Rig.TUnit.Architecture.Tests.csproj` — add `<None Include=".parity-coverage.txt"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` so the test can read it at runtime from `bin/`.
      - `README.md` — "How testing works" section paragraph on progressive parity enforcement.
      Commit: `feat(007): GREEN T003 — .parity-coverage.txt driver + csproj copy-to-output (empty initial state)`

**Phase 0 exit gate**: T000–T003 green; base-library coverage ≥ 90 line / ≥ 85 branch; `.parity-coverage.txt` exists and empty; no provider regressions.

---

## Phase 1 — Azure Service Bus

> PR: `#007-p1`. Scenarios land RED first; unit tasks flip them to GREEN in order.
> Depends on: Phase 0 merged.

### Scenario RED leads (commit order: a → b → c → d)

- [x] **T015a-RED** [P] [depends: Phase 0] Scenario: session FIFO ordering.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/SessionFifoOrderingTests.cs` (new)
      Assertion: 100 messages across 10 `SessionKey`s; `OrderingAssert.PerKeyMonotonic(listener, m => m.SessionKey!, m => /* sequence */)` passes.
      Docs: `docs/providers/service-bus.md` (create; add session-FIFO example).
      Commit: `test(007): RED T015a — session FIFO ordering`

- [x] **T015b-RED** [P] [depends: Phase 0] Scenario: partitioned topic fan-out.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/PartitionedFanoutTests.cs` (new)
      Assertion: messages with distinct `PartitionKey`s reach every partition-aware receiver.
      Docs: `docs/providers/service-bus.md`.
      Commit: `test(007): RED T015b — partitioned topic fan-out`

- [x] **T015c-RED** [P] [depends: Phase 0] Scenario: DLQ on max delivery count.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/DlqRedriveTests.cs` (new)
      Assertion: message abandoned `MaxDeliveryCount` times; `DeadLetterAssert` sees it on the DLQ.
      Docs: `docs/providers/service-bus.md`.
      Commit: `test(007): RED T015c — DLQ on max delivery count`

- [x] **T015d-RED** [P] [depends: Phase 0] Scenario: SQL filter subscription.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/SqlFilterTests.cs` (new)
      Assertion: subscription with `SqlRuleFilter("Region='EU'")` receives only EU-tagged messages.
      Docs: `docs/providers/service-bus.md`.
      Commit: `test(007): RED T015d — SQL filter subscription`

### T014 — version bump + emulator capability probe (single GREEN)

- [x] **T014** [depends: T015a-RED, T015b-RED, T015c-RED, T015d-RED] Bump `Azure.Messaging.ServiceBus` 7.18.2 → ≥ 7.20.1; add capability probe.
      Files:
      - `Directory.Packages.props` (line 121: `Version="7.20.1"`)
      - `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Topology/ServiceBusEmulatorCapabilityProbeTests.cs` (new)
      - `docs/providers/service-bus.md` — "Emulator capability table" section + any `[Skip]` annotations per C-004 if probe flags gaps.
      Commit: `feat(007): GREEN T014 — bump Azure.Messaging.ServiceBus to 7.20.1 + emulator probe (no red — version bump)`

### T010 — ServiceBusEventSender `SendContext` overload

- [x] **T010-RED** [depends: T014] Write `ServiceBusEventSenderSendContextTests`.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/ServiceBusEventSenderSendContextTests.cs` (new)
      Tests:
      - `SendAsync_WithSessionKey_PopulatesServiceBusMessageSessionId`
      - `SendAsync_WithPartitionKey_PopulatesServiceBusMessagePartitionKey`
      - `SendAsync_WithSessionAndPartitionKeyUnequal_ThrowsInvalidOperationException`
      - `SendAsync_WithDeduplicationKey_PopulatesServiceBusMessageMessageId`
      - `SendAsync_WithDefaultSendContext_BehavesLikeLegacyOverload`
      Commit: `test(007): RED T010 — ServiceBus SendContext mapping + equality validation`

- [x] **T010-GREEN** [depends: T010-RED] Add `ServiceBusEventSender.SendAsync(SendContext, …)` overload.
      Files:
      - `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs`
      - `docs/providers/service-bus.md` — session usage section; inline XML on new overload.
      Commit: `feat(007): GREEN T010 — ServiceBus SendContext mapping`

### T011 — ServiceBusSessionListener (flips T015a GREEN once paired with T010)

- [x] **T011-RED** [depends: T010-GREEN] Write `ServiceBusSessionListenerTests`.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/ServiceBusSessionListenerTests.cs` (new)
      Tests:
      - `SessionListener_10Sessions10Messages_CapturesSessionKeyOnEveryMessage`
      - `SessionListener_ParallelSessions_EachSessionHandlerSeesOwnMessagesInOrder`
      Commit: `test(007): RED T011 — ServiceBusSessionListener captures SessionKey`

- [ ] **T011-GREEN** [depends: T011-RED] Add `ServiceBusSessionListener` using `ServiceBusClient.CreateSessionProcessor`.
      Files:
      - `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs` (new)
      - `docs/providers/service-bus.md` — session listener section; inline XML on every public member.
      Commit: `feat(007): GREEN T011 — ServiceBusSessionListener (flips T015a GREEN)`

### T012 — Provider-scoped topology interfaces + admin helper (flips T015b/c/d GREEN)

- [ ] **T012-RED** [depends: T011-GREEN] Write unit + integration tests for topology.
      Files:
      - `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/Topology/ServiceBusAdministrationHelperTests.cs` (new) — mock `ServiceBusAdministrationClient`, assert expected `Create*Async` calls.
      - `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Topology/ServiceBusAdminEmulatorTests.cs` (new) — topic + subscription + DLQ + SQL filter + idempotency.
      - `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/Topology/ServiceBusBuilderCompileFenceTests.cs` (new) — reflection asserts `IServiceBusQueueConfig` declares no `.WithFifo` / `.WithQuorum` / `.WithPartitions`.
      Commit: `test(007): RED T012 — admin helper + topology builder + compile fence`

- [ ] **T012-GREEN** [depends: T012-RED] Add provider-scoped topology interfaces + impls + admin helper per C-003 (see [data-model.md §ServiceBus](data-model.md) for shape).
      Files:
      - `src/Rig.TUnit.Messaging.ServiceBus/Topology/IServiceBusTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.ServiceBus/Topology/IServiceBusTopicConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.ServiceBus/Topology/IServiceBusSubscriptionConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.ServiceBus/Topology/IServiceBusQueueConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.ServiceBus/Topology/ServiceBusTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.ServiceBus/Topology/ServiceBusAdministrationHelper.cs` (new)
      - `docs/providers/service-bus.md` — admin-client section + migration note; inline XML on every new public member.
      Commit: `feat(007): GREEN T012 — ServiceBus topology (flips T015b/c/d GREEN)`

### T013 — WithTopology hook + parity file append

- [ ] **T013-RED** [depends: T012-GREEN] Write `ServiceBusRigBuilderWithTopologyTests`.
      File: `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Topology/ServiceBusRigBuilderWithTopologyTests.cs` (new)
      Tests:
      - `WithTopology_CreatesTopicSubscriptionQueue_OnEmulator`
      - `WithTopology_CalledTwice_IsIdempotent`
      - `WithTopology_ReturnsSameBuilderForChain`
      Commit: `test(007): RED T013 — ServiceBusRigBuilder.WithTopology hook`

- [ ] **T013-GREEN** [depends: T013-RED] Add `ServiceBusRigBuilder.WithTopology(Action<IServiceBusTopologyBuilder>)` + append assembly to parity file.
      Files:
      - `src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilder.cs`
      - `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (append `Rig.TUnit.Messaging.ServiceBus`)
      - `README.md` — ServiceBus `WithTopology` minimal example.
      Commit: `feat(007): GREEN T013 — ServiceBus WithTopology + parity enforcement`

### T016 — seed file shrink (single GREEN)

- [ ] **T016** [depends: T013-GREEN] Shrink `service-bus-config.json` to namespace only.
      Files:
      - `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/TestInfrastructure/service-bus-config.json`
      - `docs/providers/service-bus.md` — "Migration from JSON seed" note.
      Commit: `feat(007): GREEN T016 — shrink service-bus-config.json to namespace only (no red — config shrink)`

**Phase 1 exit gate**: all 15 commits landed (4 RED scenarios + T014 + 5 RED/GREEN pairs for T010/T011/T012/T013 + T016); existing ServiceBus tests green; T015a–d GREEN (or `[Skip]` per C-004 with documented gap); coverage ≥ 90/≥ 85; `.parity-coverage.txt` contains `Rig.TUnit.Messaging.ServiceBus`.

---

## Phase 2 — Kafka

> PR: `#007-p2`. Depends on Phase 0 merged (does not depend on Phase 1).

### Scenario RED leads

- [ ] **T025a-RED** [P] [depends: Phase 0] Scenario: multi-partition per-key ordering.
      File: `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/MultiPartitionOrderingTests.cs` (new)
      Assertion: 6-partition topic, 5 keys × 20 messages, `OrderingAssert.PerKeyMonotonic` per key.
      Docs: `docs/providers/kafka.md`.
      Commit: `test(007): RED T025a — multi-partition per-key ordering`

- [ ] **T025b-RED** [P] [depends: Phase 0] Scenario: compacted-topic retention.
      File: `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/CompactedRetentionTests.cs` (new)
      Assertion: declare `cleanup.policy=compact`; send duplicate keys; older values compacted.
      Docs: `docs/providers/kafka.md`.
      Commit: `test(007): RED T025b — compacted-topic retention`

### T020 — KafkaEventSender `SendContext` overload

- [ ] **T020-RED** [depends: T025a-RED, T025b-RED] Write `KafkaEventSenderSendContextTests`.
      File: `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/KafkaEventSenderSendContextTests.cs` (new)
      Tests:
      - `SendAsync_WithPartitionKey_SetsMessageKey`
      - `SendAsync_WithSessionKeyOnly_FoldsToMessageKey`
      - `SendAsync_WithPartitionKeyAndCorrelationId_PrefersPartitionKey`
      - `SendAsync_LegacyOverload_Unchanged` (regression)
      Commit: `test(007): RED T020 — Kafka partition-key decoupled from correlationId`

- [ ] **T020-GREEN** [depends: T020-RED] Decouple `Message.Key` from `correlationId` at [`KafkaEventSender.cs:34`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34); add `SendAsync(SendContext, …)`.
      Files:
      - `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs`
      - `docs/providers/kafka.md` — explicit `PartitionKey` section; inline XML on new overload.
      Commit: `feat(007): GREEN T020 — Kafka SendContext + key decoupling`

### T021 — `KafkaFixtureOptions.DefaultPartitions`

- [ ] **T021-RED** [depends: T020-GREEN] Extend `KafkaFixtureOptionsTests`.
      File: `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/KafkaFixtureOptionsTests.cs` (extend)
      Tests:
      - `DefaultPartitions_NotSet_DefaultsTo1`
      - `DefaultPartitions_OutOfRange_FailsValidation`
      Commit: `test(007): RED T021 — KafkaFixtureOptions.DefaultPartitions`

- [ ] **T021-GREEN** [depends: T021-RED] Add `[Range(1, 200)] public int DefaultPartitions { get; init; } = 1;`.
      Files:
      - `src/Rig.TUnit.Messaging.Kafka/Options/KafkaFixtureOptions.cs`
      - `docs/providers/kafka.md` — options table.
      Commit: `feat(007): GREEN T021 — KafkaFixtureOptions.DefaultPartitions`

### T022 — `EnsureTopicExistsAsync` honours configs (flips T025a GREEN)

- [ ] **T022-RED** [depends: T021-GREEN] Write `KafkaTopicConfigTests`.
      File: `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/KafkaTopicConfigTests.cs` (new)
      Tests:
      - `EnsureTopicExistsAsync_WithPartitionsAndConfigs_CreatesTopicWithExactShape` — verifies via `AdminClient.DescribeConfigsAsync`.
      Commit: `test(007): RED T022 — topic creation honours partitions + configs`

- [ ] **T022-GREEN** [depends: T022-RED] Extend `EnsureTopicExistsAsync` at [`KafkaListener.cs:74`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:74) with partition count + configs dictionary.
      Files:
      - `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs`
      - `docs/providers/kafka.md` — topic-config plumbing table.
      Commit: `feat(007): GREEN T022 — EnsureTopicExistsAsync configs (flips T025a GREEN)`

### T023 — Provider-scoped topology + WithTopology + parity append (flips T025b GREEN)

- [ ] **T023-RED** [depends: T022-GREEN] Write Kafka topology tests + compile fence.
      Files:
      - `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/Topology/KafkaTopologyBuilderTests.cs` (new) — mock `AdminClient`, assert `CreateTopicsAsync` call.
      - `tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/Topology/KafkaBuilderCompileFenceTests.cs` (new) — reflection asserts `IKafkaTopologyBuilder` has no `.Queue` / `.Exchange` / `.Subscription`.
      - `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Topology/KafkaTopologyBuilderLiveTests.cs` (new) — includes `WithTopology_CalledTwice_IsIdempotent` (re-apply same declaration, assert no exception + same topic shape).
      Commit: `test(007): RED T023 — Kafka topology builder + compile fence + idempotency`

- [ ] **T023-GREEN** [depends: T023-RED] Add provider-scoped interfaces + impl + `WithTopology` + parity append.
      Files:
      - `src/Rig.TUnit.Messaging.Kafka/Topology/IKafkaTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.Kafka/Topology/IKafkaTopicConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.Kafka/Topology/KafkaTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.Kafka/Builder/KafkaRigBuilder.cs` (add `WithTopology(Action<IKafkaTopologyBuilder>)`)
      - `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (append `Rig.TUnit.Messaging.Kafka`)
      - `docs/providers/kafka.md` — `WithTopology` example; `README.md` — Kafka snippet.
      Commit: `feat(007): GREEN T023 — Kafka topology + WithTopology + parity (flips T025b GREEN)`

### T024 — Pinned-partition helper

- [ ] **T024-RED** [depends: T023-GREEN] Write `KafkaPinnedPartitionTests`.
      File: `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/KafkaPinnedPartitionTests.cs` (new)
      Test: `Assign_ToPartition3_OnlyReceivesHash3-Messages`.
      Commit: `test(007): RED T024 — KafkaListener.Assign pinned partition`

- [ ] **T024-GREEN** [depends: T024-RED] Add `KafkaListener.Assign(int partition)` helper.
      Files:
      - `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs`
      - `docs/providers/kafka.md` — pinned-partition note.
      Commit: `feat(007): GREEN T024 — KafkaListener.Assign`

**Phase 2 exit gate**: T025a+b GREEN; single-partition default-back-compat tests unchanged; coverage ≥ 90/≥ 85; `.parity-coverage.txt` contains `Rig.TUnit.Messaging.Kafka`.

---

## Phase 3 — SQS FIFO

> PR: `#007-p3`. Depends on Phase 0.

### Scenario RED leads

- [ ] **T033a-RED** [P] [depends: Phase 0] Scenario: FIFO ordering per group.
      File: `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/FifoOrderingTests.cs` (new)
      Assertion: 5 groups × 10 messages; `OrderingAssert.PerKeyMonotonic` per group.
      Docs: `docs/providers/sqs.md`.
      Commit: `test(007): RED T033a — FIFO ordering per group`

- [ ] **T033b-RED** [P] [depends: Phase 0] Scenario: DLQ redrive.
      File: `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/DlqRedriveTests.cs` (new)
      Assertion: message fails `MaxReceiveCount` times → on DLQ.
      Docs: `docs/providers/sqs.md`.
      Commit: `test(007): RED T033b — SQS DLQ redrive`

- [ ] **T033c-RED** [P] [depends: Phase 0] Scenario: content-based deduplication.
      File: `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/ContentBasedDedupTests.cs` (new)
      Assertion: duplicate body within 5-min window received once.
      Docs: `docs/providers/sqs.md`.
      Commit: `test(007): RED T033c — SQS content-based deduplication`

### T030 — SqsEventSender `SendContext` overload

- [ ] **T030-RED** [depends: T033a-RED, T033b-RED, T033c-RED] Write `SqsEventSenderSendContextTests`.
      File: `tests/Rig.TUnit.Messaging.Sqs.Tests.Unit/SqsEventSenderSendContextTests.cs` (new)
      Tests:
      - `SendAsync_FifoQueueMissingSessionKey_ThrowsInvalidOperationException`
      - `SendAsync_SessionKey_PopulatesMessageGroupId`
      - `SendAsync_DeduplicationKey_PopulatesMessageDeduplicationId`
      - `SendAsync_StandardQueue_IgnoresFifoFields`
      Commit: `test(007): RED T030 — SQS SendContext mapping + FIFO validation`

- [ ] **T030-GREEN** [depends: T030-RED] Add `SqsEventSender.SendAsync(SendContext, …)`.
      Files:
      - `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsEventSender.cs`
      - `docs/providers/sqs.md` (create) — FIFO section + `IsolationKey` prefix guidance; inline XML.
      Commit: `feat(007): GREEN T030 — SQS SendContext mapping`

### T031 — Provider-scoped topology + WithTopology + parity append (flips T033a/b/c GREEN)

- [ ] **T031-RED** [depends: T030-GREEN] Write SQS topology tests + compile fence.
      Files:
      - `tests/Rig.TUnit.Messaging.Sqs.Tests.Unit/Topology/SqsTopologyBuilderTests.cs` (new)
      - `tests/Rig.TUnit.Messaging.Sqs.Tests.Unit/Topology/SqsBuilderCompileFenceTests.cs` (new) — asserts no `.Topic`/`.Exchange`/`.Stream`/`.Subscription`.
      - `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Topology/SqsTopologyBuilderLiveTests.cs` (new) — includes `WithTopology_CalledTwice_IsIdempotent` (re-apply same declaration, assert `CreateQueueAsync` handles existing queue without throwing).
      Commit: `test(007): RED T031 — SQS topology builder + compile fence + idempotency`

- [ ] **T031-GREEN** [depends: T031-RED] Add interfaces + impl + `WithTopology` + parity append.
      Files:
      - `src/Rig.TUnit.Messaging.Sqs/Topology/ISqsTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.Sqs/Topology/ISqsQueueConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.Sqs/Topology/SqsTopologyBuilder.cs` (new) — `.WithFifo(...)` appends `.fifo` suffix.
      - `src/Rig.TUnit.Messaging.Sqs/Builder/SqsRigBuilder.cs` (add `WithTopology`)
      - `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (append `Rig.TUnit.Messaging.Sqs`)
      - `docs/providers/sqs.md` — `WithTopology` example; `README.md` — SQS snippet.
      Commit: `feat(007): GREEN T031 — SQS topology + WithTopology + parity (flips T033a/b/c GREEN)`

### T032 — Listener attributes

- [ ] **T032-RED** [depends: T031-GREEN] Write `SqsSessionListenerCaptureTests`.
      File: `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/SqsSessionListenerCaptureTests.cs` (new)
      Test: `ReceiveMessage_WithMessageGroupId_PopulatesCapturedMessageSessionKey`.
      Commit: `test(007): RED T032 — SqsListener captures MessageGroupId`

- [ ] **T032-GREEN** [depends: T032-RED] Request `MessageGroupId` + `SequenceNumber` attributes; populate `CapturedMessage.SessionKey`.
      Files:
      - `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs`
      - `docs/providers/sqs.md` — listener behaviour table.
      Commit: `feat(007): GREEN T032 — SqsListener attribute plumbing`

**Phase 3 exit gate**: T033a–c GREEN; standard-queue tests unchanged; coverage ≥ 90/≥ 85; `.parity-coverage.txt` contains `Rig.TUnit.Messaging.Sqs`.

---

## Phase 4 — RabbitMQ

> PR: `#007-p4`. Depends on Phase 0. Ships in release N+2.

### Scenario RED leads

- [ ] **T044a-RED** [P] [depends: Phase 0] Scenario: topic-exchange fan-out.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/TopicFanoutTests.cs` (new)
      Assertion: 3 queues bound on `user.*`/`order.*`/`stock.*`; each receives only its subject.
      Docs: `docs/providers/rabbitmq.md`.
      Commit: `test(007): RED T044a — topic-exchange fan-out`

- [ ] **T044b-RED** [P] [depends: Phase 0] Scenario: DLX on nack.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/DlxOnNackTests.cs` (new)
      Assertion: nacked message routes via `x-dead-letter-exchange` to DLX queue.
      Docs: `docs/providers/rabbitmq.md`.
      Commit: `test(007): RED T044b — DLX on nack`

- [ ] **T044c-RED** [P] [depends: Phase 0] Scenario: priority queue ordering.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/PriorityOrderingTests.cs` (new)
      Assertion: priority queue delivers high-priority messages first.
      Docs: `docs/providers/rabbitmq.md`.
      Commit: `test(007): RED T044c — priority queue ordering`

- [ ] **T044d-RED** [P] [depends: Phase 0] Scenario: quorum queue.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/QuorumQueueTests.cs` (new)
      Assertion: `x-queue-type=quorum` queue accepts messages and survives broker restart.
      Docs: `docs/providers/rabbitmq.md`.
      Commit: `test(007): RED T044d — quorum queue`

### T040 — RabbitMqEventSender `SendContext` + exchange/routingKey

- [ ] **T040-RED** [depends: T044a-RED, T044b-RED, T044c-RED, T044d-RED] Write `RabbitMqEventSenderSendContextTests`.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/RabbitMqEventSenderSendContextTests.cs` (new)
      Tests:
      - `SendAsync_WithExchangeAndRoutingKey_PassesToBasicPublishAsync`
      - `SendAsync_WithPartitionKey_WritesXPartitionKeyHeader`
      - `SendAsync_DefaultExchange_LegacyBehaviour` (regression)
      Commit: `test(007): RED T040 — RabbitMq SendContext + explicit exchange/routingKey`

- [ ] **T040-GREEN** [depends: T040-RED] Add overload with `exchange` + `routingKey` + `x-partition-key` header; legacy default-exchange behaviour preserved when both omitted.
      Files:
      - `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqEventSender.cs`
      - `docs/providers/rabbitmq.md` (create) — routing-key + header conventions; inline XML.
      Commit: `feat(007): GREEN T040 — RabbitMq SendContext + exchange/routingKey`

### T041 — Listener binding declaration

- [ ] **T041-RED** [depends: T040-GREEN] Write `RabbitMqBindingListenerTests`.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/RabbitMqBindingListenerTests.cs` (new)
      Test: `ListenOn_TopicExchangeWithBinding_ReceivesOnlyMatchingRoutingKey_AndPopulatesSessionKey`.
      Commit: `test(007): RED T041 — listener binding + SessionKey from x-partition-key`

- [ ] **T041-GREEN** [depends: T041-RED] Declare exchange + binding before `BasicConsumeAsync`; read `x-partition-key` into `CapturedMessage.SessionKey`.
      Files:
      - `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs`
      - `docs/providers/rabbitmq.md` — exchange + binding example.
      Commit: `feat(007): GREEN T041 — RabbitMqListener binding + SessionKey capture`

### T042 — Topology builder + WithTopology + compile fence + parity append (flips T044a/b/c/d GREEN)

- [ ] **T042-RED** [depends: T041-GREEN] Write topology tests + compile fence.
      Files:
      - `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/Topology/RabbitMqTopologyBuilderTests.cs` (new) — mock `IChannel`.
      - `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/Topology/RabbitMqBuilderCompileFenceTests.cs` (new) — asserts no `.Subscription`/`.Stream` on builder; no `.WithFifo`/`.WithRequiresSession`/`.WithPartitions` on `IRabbitMqQueueConfig`.
      - `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/RabbitMqTopologyLiveTests.cs` (new) — includes `WithTopology_CalledTwice_IsIdempotent` (re-apply exchange + queue + binding, assert no `OperationInterruptedException` from `PRECONDITION_FAILED` on conflicting args).
      Commit: `test(007): RED T042 — RabbitMq topology builder + compile fence + idempotency`

- [ ] **T042-GREEN** [depends: T042-RED] Add provider-scoped interfaces + impl + `WithTopology` + parity append.
      Files:
      - `src/Rig.TUnit.Messaging.RabbitMq/Topology/IRabbitMqTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.RabbitMq/Topology/IRabbitMqExchangeConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.RabbitMq/Topology/IRabbitMqQueueConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.RabbitMq/Topology/ExchangeType.cs` (new — enum)
      - `src/Rig.TUnit.Messaging.RabbitMq/Topology/RabbitMqTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.RabbitMq/Builder/RabbitMqRigBuilder.cs` (add `WithTopology`)
      - `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (append `Rig.TUnit.Messaging.RabbitMq`)
      - `docs/providers/rabbitmq.md` — full exchange/binding/DLX example; `README.md` — Rabbit snippet.
      Commit: `feat(007): GREEN T042 — RabbitMq topology + WithTopology + parity (flips T044a/b/c/d GREEN)`

### T043 — Queue-argument plumbing

- [ ] **T043-RED** [depends: T042-GREEN] Write `RabbitMqQueueArgsTests`.
      File: `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Unit/Topology/RabbitMqQueueArgsTests.cs` (new)
      Tests: one per `With…` method mapping to the expected AMQP argument (TTL, max-length, max-priority, DLX, DLX-routing-key, quorum).
      Commit: `test(007): RED T043 — RabbitMq queue argument plumbing`

- [ ] **T043-GREEN** [depends: T043-RED] Implement `.WithMessageTtl`, `.WithMaxLength`, `.WithMaxPriority`, `.WithDeadLetterExchange`, `.WithQuorum` on `RabbitMqQueueConfig`.
      Files:
      - `src/Rig.TUnit.Messaging.RabbitMq/Topology/RabbitMqQueueConfig.cs` (sealed impl of `IRabbitMqQueueConfig` — added in T042, methods filled in T043).
      - `docs/providers/rabbitmq.md` — queue-args reference table.
      Commit: `feat(007): GREEN T043 — RabbitMq queue-args plumbing`

**Phase 4 exit gate**: T044a–d GREEN; existing default-exchange tests unchanged; coverage ≥ 90/≥ 85; `.parity-coverage.txt` contains `Rig.TUnit.Messaging.RabbitMq`.

---

## Phase 5 — NATS JetStream

> PR: `#007-p5`. Depends on Phase 0. Ships in release N+3.
> Core-NATS fixture stays untouched.

### T050 — package + dependency guard (single GREEN)

- [ ] **T050** [depends: Phase 0] Add `NATS.Client.JetStream` package + extend `DependencyDirectionTests`.
      Files:
      - `Directory.Packages.props` (add `<PackageVersion Include="NATS.Client.JetStream" Version="2.5.0" />`)
      - `src/Rig.TUnit.Messaging.Nats/Rig.TUnit.Messaging.Nats.csproj` (`<PackageReference Include="NATS.Client.JetStream" />`)
      - `tests/Rig.TUnit.Architecture.Tests/Rules/DependencyDirectionTests.cs` (extend: assert `NATS.Client.JetStream` referenced only by the Nats `.csproj`).
      - `docs/providers/nats.md` (create — dependency note).
      Commit: `feat(007): GREEN T050 — NATS.Client.JetStream package + architecture guard (no red — package ref + assertion land together)`

### Scenario RED leads

- [ ] **T055a-RED** [P] [depends: T050] Scenario: ordered delivery across reconnects.
      File: `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/OrderedReconnectTests.cs` (new)
      Assertion: ordered consumer survives a brief disconnect without duplicates.
      Docs: `docs/providers/nats.md`.
      Commit: `test(007): RED T055a — JetStream ordered reconnect`

- [ ] **T055b-RED** [P] [depends: T050] Scenario: multi-subject filter.
      File: `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/MultiSubjectFilterTests.cs` (new)
      Assertion: consumer with `FilterSubjects("a.*", "b.*")` only sees those subjects.
      Docs: `docs/providers/nats.md`.
      Commit: `test(007): RED T055b — JetStream multi-subject filter`

- [ ] **T055c-RED** [P] [depends: T050] Scenario: retention policy.
      File: `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/RetentionPolicyTests.cs` (new)
      Assertion: stream with `RetentionPolicy.Limits` + `MaxMsgs=10` drops oldest.
      Docs: `docs/providers/nats.md`.
      Commit: `test(007): RED T055c — JetStream retention policy`

### T051 — NatsJetStreamFixture

- [ ] **T051-RED** [depends: T050, T055a-RED, T055b-RED, T055c-RED] Write `NatsJetStreamFixtureTests`.
      File: `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/NatsJetStreamFixtureTests.cs` (new)
      Tests: lifecycle, connection opens, JetStream context reachable.
      Commit: `test(007): RED T051 — NatsJetStreamFixture lifecycle`

- [ ] **T051-GREEN** [depends: T051-RED] Add `NatsJetStreamFixture` alongside existing `NatsFixture`.
      Files:
      - `src/Rig.TUnit.Messaging.Nats/Fixtures/NatsJetStreamFixture.cs` (new)
      - `docs/providers/nats.md` — core vs JetStream fixture split; inline XML.
      Commit: `feat(007): GREEN T051 — NatsJetStreamFixture`

### T052 — NatsJetStreamEventSender

- [ ] **T052-RED** [depends: T051-GREEN] Write sender tests.
      Files:
      - `tests/Rig.TUnit.Messaging.Nats.Tests.Unit/NatsJetStreamEventSenderTests.cs` (new) — mock `INatsJSContext`.
      - `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/NatsJetStreamSenderLiveTests.cs` (new).
      Commit: `test(007): RED T052 — NatsJetStreamEventSender SendContext mapping`

- [ ] **T052-GREEN** [depends: T052-RED] Add `NatsJetStreamEventSender` via `INatsJSContext.PublishAsync`.
      Files:
      - `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs` (new)
      - `docs/providers/nats.md` — JetStream send example.
      Commit: `feat(007): GREEN T052 — NatsJetStreamEventSender`

### T053 — NatsJetStreamListener (flips T055a GREEN)

- [ ] **T053-RED** [depends: T052-GREEN] Write listener tests.
      File: `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/NatsJetStreamListenerTests.cs` (new)
      Tests:
      - `OrderedConsumer_RecordsSessionKeyFromSubjectSegment`
      - `OrderedConsumer_NoDuplicatesAcrossAck`
      Commit: `test(007): RED T053 — NatsJetStreamListener ordered consumer`

- [ ] **T053-GREEN** [depends: T053-RED] Add listener with `DeliverPolicy.All + ReplayPolicy.Instant + FlowControl=true + AckPolicy.Explicit`.
      Files:
      - `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs` (new)
      - `docs/providers/nats.md` — ordered-consumer example.
      Commit: `feat(007): GREEN T053 — NatsJetStreamListener (flips T055a GREEN)`

### T054 — Topology builder + WithTopology + parity append (flips T055b/c GREEN)

- [ ] **T054-RED** [depends: T053-GREEN] Write topology tests + compile fence.
      Files:
      - `tests/Rig.TUnit.Messaging.Nats.Tests.Unit/Topology/NatsTopologyBuilderTests.cs` (new)
      - `tests/Rig.TUnit.Messaging.Nats.Tests.Unit/Topology/NatsBuilderCompileFenceTests.cs` (new) — asserts `INatsTopologyBuilder` has no `.Queue`/`.Topic`/`.Exchange`/`.Subscription`.
      - `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/NatsTopologyBuilderLiveTests.cs` (new) — includes `WithTopology_CalledTwice_IsIdempotent` (re-apply stream + consumer, assert `CreateStreamAsync` / `CreateConsumerAsync` handle existing entities without throwing).
      Commit: `test(007): RED T054 — NATS topology builder + compile fence + idempotency`

- [ ] **T054-GREEN** [depends: T054-RED] Add provider-scoped interfaces + impl + `WithTopology` + parity append.
      Files:
      - `src/Rig.TUnit.Messaging.Nats/Topology/INatsTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.Nats/Topology/INatsStreamConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.Nats/Topology/INatsConsumerConfig.cs` (new)
      - `src/Rig.TUnit.Messaging.Nats/Topology/NatsTopologyBuilder.cs` (new)
      - `src/Rig.TUnit.Messaging.Nats/Builder/NatsRigBuilder.cs` (add `WithTopology`)
      - `tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt` (append `Rig.TUnit.Messaging.Nats` — now fully populated)
      - `docs/providers/nats.md` — `WithTopology` example; `README.md` — NATS snippet.
      Commit: `feat(007): GREEN T054 — NATS topology + WithTopology + parity (flips T055b/c GREEN)`

**Phase 5 exit gate**: core-NATS fixture untouched and green; JetStream suite green in its own CI matrix entry; T055a–c GREEN; coverage ≥ 90/≥ 85; `.parity-coverage.txt` fully populated (all 5 providers).

---

## Phase 6 — Documentation & benchmarks

> Runs in parallel with whichever release is shipping. May overlap with any provider phase's exit gate.

- [ ] **T060** [depends: at least one provider phase shipped] Top-level README messaging section + cross-link audit.
      File: `README.md`
      Change: add "Messaging topology & sessions" section with a minimal per-provider example for every provider that has shipped in the current release.
      Commit: `docs(007): GREEN T060 — README messaging topology & sessions section (no red — docs-only)`

- [ ] **T061** [depends: T060 per-release; depends: that release's provider phases GREEN] CHANGELOG entry per shipped release — one entry for each release containing phases, **not one batched entry** per NFR-C5.
      File: `CHANGELOG.md`
      Sub-tasks: T061-N1 (release N+1: Phases 0+1+2+3), T061-N2 (release N+2: Phase 4), T061-N3 (release N+3: Phase 5).
      Commit: one per release — e.g. `docs(007): GREEN T061-N1 — CHANGELOG entry for release N+1 (no red — docs-only)`

- [ ] **T062** [depends: T013-GREEN, T023-GREEN] Benchmarks for ServiceBus session vs non-session + Kafka multi-partition per-key.
      Files:
      - `tests/Rig.TUnit.Benchmarks/ServiceBusMessagingBenchmarks.cs` (extend — add `SessionProcessor_VsNonSession_Throughput`)
      - `tests/Rig.TUnit.Benchmarks/KafkaMessagingBenchmarks.cs` (extend — add `MultiPartition_PerKey_Throughput`)
      - `benchmarks/baseline-007.json` (new — populated by benchmark run)
      - `docs/providers/service-bus.md` — benchmark reference
      - `docs/providers/kafka.md` — benchmark reference
      Commit: `feat(007): GREEN T062 — benchmarks: ServiceBus session + Kafka multi-partition (no red — benchmark additions)`

- [ ] **T063** [depends: all provider phases shipped] Update `OrderingAssert` XML docs with provider capability matrix; mirror in `docs/ordering-assertions.md`.
      Files:
      - `src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs` (XML doc block only — no signature change)
      - `docs/ordering-assertions.md` (update with the capability matrix)
      Commit: `docs(007): GREEN T063 — OrderingAssert capability matrix (no red — docs-only)`

---

## Task count summary

| Phase | Tasks | Includes |
|-------|-------|----------|
| 0 — base library | 7 | T000/T001/T003 × (RED+GREEN) + T002 single-GREEN |
| 1 — ServiceBus | 14 | T015a/b/c/d RED + T014 single + T010/T011/T012/T013 × (RED+GREEN) + T016 single |
| 2 — Kafka | 12 | T025a/b RED + T020/T021/T022/T023/T024 × (RED+GREEN) |
| 3 — SQS | 9 | T033a/b/c RED + T030/T031/T032 × (RED+GREEN) |
| 4 — RabbitMQ | 12 | T044a/b/c/d RED + T040/T041/T042/T043 × (RED+GREEN) |
| 5 — NATS JetStream | 12 | T050 single + T055a/b/c RED + T051/T052/T053/T054 × (RED+GREEN) |
| 6 — Docs & benchmarks | 6 | T060, T061-N1/-N2/-N3, T062, T063 |
| **Total** | **72** | — |

**Parallel opportunities**: 18 tasks marked `[P]` — all the scenario RED leads across Phases 1-5.

**Critical path** (serial worst case):
Phase 0 (8 tasks, ~1 d) → Phase 1 (14 tasks, ~3 d) → Phase 6 T060+T061-N1+T062 (release N+1 doc/bench)
→ Phase 4 (12 tasks, ~2 d) → Phase 6 T061-N2 (release N+2)
→ Phase 5 (12 tasks, ~2.5 d) → Phase 6 T061-N3+T063 (release N+3)
= ~9 d serial.

**Parallel** (2 devs, Phases 2+3 concurrent with Phase 1; Phase 4 concurrent with Phases 2+3):
= ~7 d per plan §Rollout Envelope.

---

## Next

1. `/dotnet-ai-kit:analyze` — optional consistency check (spec ↔ plan ↔ tasks).
2. `/dotnet-ai-kit:implement` — start with Phase 0 (T000-RED → T000-GREEN → T001-RED → … → T003-GREEN), ship as PR `#007-p0`.
3. After Phase 0 merges, open provider-phase sub-branches in parallel per rollout envelope.
