# Implementation Plan — 007-messaging-topology-sessions

**Feature**: Messaging Topology & Sessions
**Branch**: `feat/007-messaging-topology-sessions`
**Mode**: Generic / single-repo (messaging sub-tree only)
**Complexity**: Complex (7 FRs active, 6 phases, 5 provider integrations, 64 tasks, 3-release rollout)
**Generated**: 2026-04-23
**Spec**: [spec.md](spec.md) · **Research**: [research.md](research.md) · **Data model**: [data-model.md](data-model.md) · **Quickstart**: [quickstart.md](quickstart.md) · **Contracts**: [contracts/](contracts/)

---

## Constitution Check

`.dotnet-ai-kit/memory/constitution.md` does not exist. Proceeding without the formal gate — run `/dai.learn` after the feature completes to generate one.

Key detected conventions applied in this plan (lifted from the existing codebase, not invented):

- **.NET 10 / C# 14** (`net10.0` in `global.json`; C# 14 language features permitted).
- **TUnit** testing framework (`[Test]`, `Assert.That(x).IsEqualTo(y)`, `async Task` test methods).
- **`{Method}_{Scenario}_{ExpectedResult}`** test naming (confirmed in existing Messaging tests).
- **Arrange-Act-Assert** with blank-line separation.
- `RigConnect.FromValue()` for no-container builder tests.
- `services.AddRigTUnit(rig => captured = rig)` DI-capture pattern.
- **Provider-parity architecture test** — re-use `ProviderCompletenessTests.cs` (per planning/provider-consistency-remediation).
- **Central Package Management** (`Directory.Packages.props`) — version bumps only land there.
- **File-scoped namespaces**, `sealed` concrete types, `record` for value objects, **nullable reference types enabled**.
- **No `[Obsolete]`/back-compat shims** — packages are pre-release (memory: `feedback_unreleased_no_backcompat.md`).
- **Feature spec lives in** `.dotnet-ai-kit/features/NNN-name/spec.md` only — planning folder is design input (memory: `feedback_spec_home_is_sdd_feature_folder.md`).
- **Compile-time safety over runtime fall-through** (memory: `feedback_compile_time_over_runtime.md`).

---

## Complexity Tracking

| Dimension | Measurement | Level |
|-----------|-------------|-------|
| Entities / new public types | ≥ 20 (base `SendContext` + `ITopologyBuilder`; 5 × `I{Provider}TopologyBuilder`; 10+ config interfaces; 5 × topology-builder impls; `ServiceBusSessionListener`; `NatsJetStreamFixture`/`Sender`/`Listener`) | HIGH |
| External service integrations | 5 (ServiceBus emulator, Kafka, RabbitMQ, NATS JetStream, LocalStack/SQS) | HIGH |
| Multi-repo | No — single repo (`Rig.TUnit`) | Low |
| Functional requirements | 7 active (FR-007-01…07; FR-007-08 superseded by C-000) | Medium |
| Data migrations / state transitions | No (test-only; rig does not hold persistent state) | Low |

Complexity rating **HIGH** → full artefact set (plan.md + research.md + data-model.md + quickstart.md + contracts/).

### No-gap statement

No spec-level rule is being broken by this plan. All clauses below trace back to the spec §Task List or a clarification decision C-000 … C-005. Any apparent violation (e.g. "additive-only public API") is an obsolete NFR superseded by C-000 and flagged as such in the spec.

---

## Technical Context

### Affected source trees

Source packages this plan modifies or extends (relative to repo root):

| Package | Role in this feature | Touches |
|---------|----------------------|---------|
| `src/Rig.TUnit.Messaging/` | Base contracts (`SendContext`, `ITopologyBuilder`, `CapturedMessage<TMessage>.SessionKey`). | Phase 0 (T000–T002). |
| `src/Rig.TUnit.Messaging.ServiceBus/` | Session listener, admin-client topology, provider-scoped interfaces, `WithTopology` hook. | Phase 1 (T010–T016). |
| `src/Rig.TUnit.Messaging.Kafka/` | `PartitionKey` decoupling, `DefaultPartitions`, topology builder, config plumbing. | Phase 2 (T020–T024). |
| `src/Rig.TUnit.Messaging.Sqs/` | FIFO send context, FIFO/DLQ topology builder, session listener. | Phase 3 (T030–T032). |
| `src/Rig.TUnit.Messaging.RabbitMq/` | Exchange/binding/DLX topology, `SendContext` with routing key + header, listener binding declare. | Phase 4 (T040–T043). |
| `src/Rig.TUnit.Messaging.Nats/` | NEW JetStream fixture / sender / listener / topology — alongside existing core NATS. | Phase 5 (T050–T054). |
| `tests/Rig.TUnit.Architecture.Tests/` | Parity assertion extensions + JetStream dependency guard + `.parity-coverage.txt` driver. | Phase 0 (T003), Phase 5 (T050). |
| `tests/Rig.TUnit.Benchmarks/` | Session-vs-non-session and multi-partition benchmarks. | Phase 6 (T062). |
| `tests/Rig.TUnit.Messaging.*.Tests.Unit` + `.Tests.Integration` | New unit and integration test projects per phase. | Every phase. |
| Top-level `README.md`, **per-package `src/Rig.TUnit.Messaging*/README.md` (1 family base + 5 providers)**, `docs/providers/*.md` (5 files, some new), `docs/glossary.md`, `docs/ordering-assertions.md`, `CHANGELOG.md`, `Directory.Packages.props` | Documentation + dependency bumps. | Every phase that changes public API; per-package READMEs swept in T064 if review finds them stale. |

### New NuGet dependencies

| Package | Version | Scope | Task |
|---------|---------|-------|------|
| `Azure.Messaging.ServiceBus` | Bump 7.18.2 → **≥ 7.20.1** | `Directory.Packages.props` only; referenced by existing ServiceBus project. | T014 |
| `NATS.Client.JetStream` | **≥ 2.5.0** (matches `NATS.Client.Core`) | Declared in `Directory.Packages.props`; referenced **only** by `Rig.TUnit.Messaging.Nats.csproj`. Enforced by `DependencyDirectionTests` extension (T050). | T050 |

No other new packages.

### Invariants

- Every public type introduced by this feature ships with inline XML doc comments (NFR-C3, required for clean docfx build).
- Every config / topology-builder method exposes only what its broker supports (C-003). Unsupported = compile error, never runtime throw, never silent no-op.
- Every integration test uses `IsolationKey` for per-test namespace prefixing (already the pattern in `MessagingFixtureBase`). SQS FIFO tests additionally prefix `DeduplicationKey`.
- Branch `feat/007-messaging-topology-sessions`, no `--no-verify`, no amends across RED/GREEN, no destructive git operations without user approval.

---

## Rollout Envelope

Per spec §Rollout and [Advantages.md §5](../../../planning/messaging-topology-and-sessions/Advantages.md):

| Release | Phases | Effort | Contents |
|---------|--------|--------|----------|
| **N+1** (ships first) | 0, 1, 2, 3, 6 (partial) | ≈ 7 d serial | Base contracts; ServiceBus sessions + admin; Kafka `PartitionKey`; SQS FIFO; docs + bench delta. |
| **N+2** | 4, 6 (partial) | ≈ 2 d | RabbitMQ exchange / DLX / priority / quorum; incremental CHANGELOG. |
| **N+3** | 5, 6 (final) | ≈ 2.5 d | NATS JetStream fixture; final docs + ordering-assertions capability matrix. |

Each release carries its own `CHANGELOG.md` entry (NFR-C5) and its own benchmark delta. Minor versions are additive only **at the package level** (a package that never shipped cannot break back-compat on a pre-release timeline; C-000).

---

## Phase Plan

Each phase below mirrors the spec §Task List and adds: work sequencing, branching, PR cadence, exit artefacts.

### Phase 0 — Cross-cutting base library (1 d)

**Gate**: blocks every other phase. Ships as a single PR on `feat/007-messaging-topology-sessions`.

**Work sequence (outside-in TDD)**:
1. `test(007): RED T000` — `SendContextTests` asserts record shape + `BuildHeaders(ctx)` parity with old overload on default context.
2. `feat(007): GREEN T000` — add `src/Rig.TUnit.Messaging/Helpers/SendContext.cs`; extend `EventSenderBase.BuildHeaders` with new overload; update `CapturedMessage<TMessage>` (`string? Body` → `string Body`, add trailing `string? SessionKey = null` per C-001); **ripple: coerce `null → string.Empty` at the 3 provider listener call sites that pass nullable bodies** (Kafka [KafkaListener.cs:151](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs), NATS [NatsListener.cs:84](../../../src/Rig.TUnit.Messaging.Nats/Helpers/NatsListener.cs), SQS [SqsListener.cs:91](../../../src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs) — ServiceBus + RabbitMQ already pass non-null). Phase 0 PR scope widens to 8 files to keep the solution compiling after the record narrowing — identified by analysis pass (see [analysis.md §HIGH](analysis.md)).
3. `test(007): RED T001` — `ITopologyBuilderContractTests` asserts the marker exposes only `ApplyAsync` (regression guard per C-003 — no fluent methods).
4. `feat(007): GREEN T001` — add `src/Rig.TUnit.Messaging/Topology/ITopologyBuilder.cs` (marker only).
5. `feat(007): GREEN T002` (single — structural assertion has no RED state) — add `MessagingRigBuilderNoGenericWithTopologyTests` + XML note on the base class.
6. `test(007): RED T003` — extend `ProviderCompletenessTests` to read `.parity-coverage.txt` and assert every listed assembly satisfies the parity contract. Landing empty so it passes vacuously.
7. `feat(007): GREEN T003` — add `.parity-coverage.txt` (empty) + `<None Include=".parity-coverage.txt"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` in `Rig.TUnit.Architecture.Tests.csproj` so the test reads it at runtime from `bin/`; land the extended test green.
8. Inline XML on every new public type; README messaging-section intro paragraph; `docs/ordering-assertions.md` created (stub).

**Exit gate**: base package compiles; T000–T003 green; `.parity-coverage.txt` present and empty; no provider regressions; base library coverage ≥ 90 line / ≥ 85 branch.

**PR**: #007-p0 — `feat(007): Phase 0 — cross-cutting base library`.

### Phase 1 — Azure Service Bus (3 d)

**Gate**: blocks releases N+1 / N+2 / N+3 for ServiceBus features. One PR.

**Work sequence (per C-002 per-scenario RED-first)**:
1. RED T015a (`SessionFifoOrderingTests`), T015b (`PartitionedFanoutTests`), T015c (`DlqRedriveTests`), T015d (`SqlFilterTests`) — four separate commits, tests fail because `SendContext`/`ServiceBusSessionListener`/topology admin don't exist yet.
2. T014 — bump `Azure.Messaging.ServiceBus` to `≥ 7.20.1` + land `ServiceBusEmulatorCapabilityProbeTests` (informational, `Assert.Inconclusive` for gaps). Single GREEN commit.
3. RED T010 → GREEN T010 — `ServiceBusEventSender.SendAsync(SendContext,…)` + equality validation.
4. RED T011 → GREEN T011 — `ServiceBusSessionListener` using `CreateSessionProcessor`. Flips T015a RED → GREEN.
5. RED T012 → GREEN T012 — `IServiceBusTopologyBuilder`, `IServiceBusTopicConfig`, `IServiceBusSubscriptionConfig`, `IServiceBusQueueConfig` + `ServiceBusAdministrationHelper` + `ServiceBusTopologyBuilder`. Flips T015b/c/d RED → GREEN.
6. RED T013 → GREEN T013 — `ServiceBusRigBuilder.WithTopology(Action<IServiceBusTopologyBuilder>)` + compile-fence unit test (asserts `.WithFifo()` absent on `IServiceBusQueueConfig`). Append `Rig.TUnit.Messaging.ServiceBus` to `.parity-coverage.txt` in the GREEN commit.
7. Single GREEN T016 — shrink `service-bus-config.json` to namespace only.
8. Emulator gap handling: if T014 probe reports any gap, the corresponding scenario gets `[Skip("emulator-gap: <op> — see docs/providers/service-bus.md#emulator-gaps")]`, gap row added to `docs/providers/service-bus.md`, upstream issue filed (C-004).

**Exit gate**: existing ServiceBus suite green; T015a–d GREEN (or explicitly `[Skip]` per C-004); coverage ≥ 90/≥ 85; `.parity-coverage.txt` now lists `Rig.TUnit.Messaging.ServiceBus`.

**PR**: #007-p1 — `feat(007): Phase 1 — ServiceBus sessions + admin topology`.

### Phase 2 — Kafka (1.5 d)

**Work sequence**:
1. RED T025a (`MultiPartitionOrderingTests`), T025b (`CompactedRetentionTests`) — two separate commits.
2. RED T020 → GREEN T020 — decouple `Message.Key` from `correlationId` in [`KafkaEventSender.cs:34`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs:34). Fallback chain: `PartitionKey ?? SessionKey ?? correlationId ?? Guid`.
3. RED T021 → GREEN T021 — `KafkaFixtureOptions.DefaultPartitions = 1` with `[Range(1, 200)]`.
4. RED T022 → GREEN T022 — `EnsureTopicExistsAsync` honours partitions + configs (retention, compaction, min-ISR). Flips T025a RED → GREEN.
5. RED T023 → GREEN T023 — `IKafkaTopologyBuilder` + `IKafkaTopicConfig` (+ compile-fence unit test asserting absence of `.Queue`/`.Exchange`/`.Subscription`). `KafkaTopologyBuilder` wired to `AdminClient.CreateTopicsAsync`. `KafkaRigBuilder.WithTopology(Action<IKafkaTopologyBuilder>)`. Flips T025b GREEN. Append to `.parity-coverage.txt`.
6. RED T024 → GREEN T024 — optional pinned-partition helper on `KafkaListener`.

**Exit gate**: existing single-partition tests unchanged; T025a & T025b GREEN; coverage ≥ 90/≥ 85; `.parity-coverage.txt` now lists `Rig.TUnit.Messaging.Kafka`.

**PR**: #007-p2 — `feat(007): Phase 2 — Kafka partition-key decoupling + topology`.

### Phase 3 — SQS FIFO (1.5 d)

**Work sequence**:
1. RED T033a (`FifoOrderingTests`), T033b (`DlqRedriveTests`), T033c (`ContentBasedDedupTests`).
2. RED T030 → GREEN T030 — `SqsEventSender.SendAsync(SendContext,…)` + FIFO-queue + missing-`SessionKey` validation. Inline `IsolationKey`-prefixed `DeduplicationKey` guidance in docs (mitigates R4 — 5-min dedup-window flake).
3. RED T031 → GREEN T031 — `ISqsTopologyBuilder` + `ISqsQueueConfig` (+ compile-fence asserting no `.Topic`/`.Exchange`/`.Stream`). `SqsTopologyBuilder` wired to `CreateQueueAsync` with FIFO attribute mapping + `.fifo` suffix appending. `SqsRigBuilder.WithTopology(...)`. Flips all three T033 scenarios GREEN. Append to `.parity-coverage.txt`.
4. RED T032 → GREEN T032 — `SqsListener` requests `MessageGroupId` + `SequenceNumber` attributes; `CapturedMessage.SessionKey` populated.

**Exit gate**: LocalStack suite green; T033a–c GREEN; coverage ≥ 90/≥ 85; `.parity-coverage.txt` lists `Rig.TUnit.Messaging.Sqs`.

**PR**: #007-p3 — `feat(007): Phase 3 — SQS FIFO + MessageGroupId`.

### Phase 4 — RabbitMQ (2 d) — ships in release N+2

**Work sequence**:
1. RED T044a (`TopicFanoutTests`), T044b (`DlxOnNackTests`), T044c (`PriorityOrderingTests`), T044d (`QuorumQueueTests`).
2. RED T040 → GREEN T040 — sender accepts `SendContext` + explicit `exchange` + `routingKey`; writes `x-partition-key` header so listener can recover key (broker strips routing key before delivery).
3. RED T041 → GREEN T041 — listener declares exchange + binding before `BasicConsumeAsync`; populates `CapturedMessage.SessionKey` from `x-partition-key`.
4. RED T042 → GREEN T042 — `IRabbitMqTopologyBuilder`, `IRabbitMqExchangeConfig`, `IRabbitMqQueueConfig` (+ compile fence asserting no `.Subscription`/`.Stream`; no `.WithFifo`/`.WithRequiresSession`). `RabbitMqTopologyBuilder` wired. `RabbitMqRigBuilder.WithTopology`. Flips T044a–d GREEN. Append to `.parity-coverage.txt`.
5. RED T043 → GREEN T043 — queue-argument plumbing (TTL, max-length, max-priority, DLX, quorum).

**Exit gate**: existing default-exchange tests unchanged; T044a–d GREEN; coverage ≥ 90/≥ 85; `.parity-coverage.txt` lists `Rig.TUnit.Messaging.RabbitMq`.

**PR**: #007-p4 — `feat(007): Phase 4 — RabbitMQ exchanges + DLX + quorum queues`.

### Phase 5 — NATS JetStream (2.5 d) — ships in release N+3

**Work sequence**:
1. Single GREEN T050 — `NATS.Client.JetStream` package + `DependencyDirectionTests` extension guard.
2. RED T055a (`OrderedReconnectTests`), T055b (`MultiSubjectFilterTests`), T055c (`RetentionPolicyTests`).
3. RED T051 → GREEN T051 — `NatsJetStreamFixture` (alongside existing `NatsFixture`, not replacing).
4. RED T052 → GREEN T052 — `NatsJetStreamEventSender` via `INatsJSContext.PublishAsync`.
5. RED T053 → GREEN T053 — `NatsJetStreamListener` with ordered consumer. Flips T055a GREEN.
6. RED T054 → GREEN T054 — `INatsTopologyBuilder`, `INatsStreamConfig`, `INatsConsumerConfig` (+ compile fence asserting no `.Queue`/`.Topic`/`.Exchange`). `NatsTopologyBuilder` wired to `CreateStreamAsync` / `CreateConsumerAsync`. `NatsRigBuilder.WithTopology`. Flips T055b/c GREEN. Append to `.parity-coverage.txt`.

**Exit gate**: core-NATS fixture untouched and green; JetStream suite green in its own CI matrix entry; T055a–c GREEN; coverage ≥ 90/≥ 85; `.parity-coverage.txt` fully populated — parity test now enforcing all 5 providers.

**PR**: #007-p5 — `feat(007): Phase 5 — NATS JetStream fixture + topology`.

### Phase 6 — Documentation & benchmarks (1 d)

Shipped **in parts** — each provider phase carries its own per-provider doc update (NFR-C3). Phase 6 is the consolidation and benchmark pass.

- T060 — top-level `README.md` messaging section + per-provider cross-link audit.
- T061 — `CHANGELOG.md` entry **per shipped release** (N+1, N+2, N+3 — three entries, not one).
- T062 — `ServiceBusMessagingBenchmarks.cs` (session vs non-session) + `KafkaMessagingBenchmarks.cs` (multi-partition per-key); populate `benchmarks/baseline-007.json` with ≥ 2 scenarios.
- T063 — `OrderingAssert` XML docs + `docs/ordering-assertions.md` provider capability matrix.
- **T064 (added 2026-04-25, post-T060 review)** — per-package READMEs sweep. T060 only touched the top-level `README.md`; the family-base `src/Rig.TUnit.Messaging/README.md` and the five `src/Rig.TUnit.Messaging.{Provider}/README.md` files still described the pre-Feature-007 surface. T064 extends each of the six READMEs (preserving the 14-section canonical structure enforced by `ReadmeCompletenessTests`) plus expands the top-level README with admin-helpers + capability matrices, plus adds matching `docs/glossary.md` entries.

**PRs**: split across releases — `#007-docs-n1`, `#007-docs-n2`, `#007-docs-n3` (or consolidated under each phase's PR, reviewer preference). T064 ships as a single docs-only follow-up PR after all provider phases are GREEN (release N+3 or later).

---

## Progressive Parity Enforcement (C-005)

Per C-005, the parity test is always truthful about landed providers:

```
Phase 0 lands:      .parity-coverage.txt (empty)           → test passes vacuously
Phase 1 GREEN T013: append Rig.TUnit.Messaging.ServiceBus  → test enforces ServiceBus
Phase 2 GREEN T023: append Rig.TUnit.Messaging.Kafka        → test enforces ServiceBus + Kafka
Phase 3 GREEN T031: append Rig.TUnit.Messaging.Sqs          → test enforces ServiceBus + Kafka + Sqs
Phase 4 GREEN T042: append Rig.TUnit.Messaging.RabbitMq     → ServiceBus + Kafka + Sqs + RabbitMq
Phase 5 GREEN T054: append Rig.TUnit.Messaging.Nats         → all five providers enforced
```

The file diff in each provider's GREEN PR is the visible rollout signal. `master` is never RED from the parity test.

---

## Risk Register (lifted and closed from spec §Risks)

| # | Owner phase | Status / mitigation applied in this plan |
|---|-------------|------------------------------------------|
| R1 | Phase 1 | `T014` capability probe runs **before** T010; per C-004 unsupported ops route to `[Skip]` + documented gap + upstream issue. |
| R2 | Phase 2 | Re-use existing `partitionsAssigned` TCS pattern from [`KafkaListener.cs:66`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:66) in all new multi-partition tests. |
| R3 | Phase 5 / DevOps | Add JetStream-only CI matrix row; skip on PRs that only touch core-NATS paths. Document in `docs/providers/nats.md`. |
| R4 | Phase 3 | Docs mandate `IsolationKey`-prefixed `DeduplicationKey`; T030 validator throws if FIFO queue + missing `SessionKey`. |
| R5 | T003 owner | Parity test enforces every assembly in `.parity-coverage.txt` (C-005); removing a line without documented justification is a reviewer-caught regression. |
| R6 | Reviewer (per PR) | Every "single GREEN" task reviewer-flagged (T014, T016, T050, T060–T064). PR checklist confirms no out-of-declared-scope production file modified. Docs-only tasks must update **both** the top-level `README.md` and every affected per-package README (T060 review-gap finding → T064). |
| R7 | Phase 6 | Benchmarks land in the existing `tests/Rig.TUnit.Benchmarks/` project (Q-4 resolution). No new project. |

---

## Test & Coverage Strategy

### Per-package coverage plan

(Reference [spec.md §Coverage Plan](spec.md) — the per-package table is authoritative.)

The reviewer rule is strict: **every new public type must be 100 % line-covered in the PR that introduces it.** Codecov diff is the gate.

### Test project matrix

| Phase | New unit tests | New integration tests | New architecture tests |
|-------|----------------|-----------------------|------------------------|
| 0 | `SendContextTests`, `ITopologyBuilderContractTests`, `MessagingRigBuilderNoGenericWithTopologyTests` | — | `ProviderCompletenessTests` extension + `.parity-coverage.txt` driver (empty at start) |
| 1 | `ServiceBusEventSenderSendContextTests`, `ServiceBusAdministrationHelperTests`, `ServiceBusBuilderCompileFenceTests` | `ServiceBusSessionListenerTests`, `ServiceBusAdminEmulatorTests`, `ServiceBusRigBuilderWithTopologyTests`, `ServiceBusEmulatorCapabilityProbeTests`, T015a–d scenarios | — |
| 2 | `KafkaEventSenderSendContextTests`, `KafkaFixtureOptionsTests` (adds), `KafkaTopologyBuilderTests`, `KafkaBuilderCompileFenceTests` | `KafkaTopicConfigTests`, `KafkaPinnedPartitionTests`, `KafkaTopologyBuilderLiveTests`, T025a–b scenarios | — |
| 3 | `SqsEventSenderSendContextTests`, `SqsTopologyBuilderTests`, `SqsBuilderCompileFenceTests` | `SqsTopologyBuilderLiveTests`, `SqsSessionListenerCaptureTests`, T033a–c scenarios | — |
| 4 | `RabbitMqEventSenderSendContextTests`, `RabbitMqTopologyBuilderTests`, `RabbitMqQueueArgsTests`, `RabbitMqBuilderCompileFenceTests` | `RabbitMqBindingListenerTests`, `RabbitMqTopologyLiveTests`, T044a–d scenarios | — |
| 5 | `NatsJetStreamEventSenderTests`, `NatsTopologyBuilderTests`, `NatsBuilderCompileFenceTests` | `NatsJetStreamFixtureTests`, `NatsJetStreamSenderLiveTests`, `NatsJetStreamListenerTests`, `NatsTopologyBuilderLiveTests`, T055a–c scenarios | `DependencyDirectionTests` extension for JetStream package guard |
| 6 | — | — | — (docs + bench only) |

### Compile-fence unit tests (per C-003)

One per provider. Each uses `Type.GetMethods()` reflection to assert the provider's topology-builder / config interfaces **do not** declare methods they shouldn't:

```csharp
// Example — tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/Topology/KafkaBuilderCompileFenceTests.cs
[Test]
public async Task IKafkaTopologyBuilder_DoesNotDeclareQueueOrExchangeOrSubscription()
{
    var iface = typeof(IKafkaTopologyBuilder);
    var forbidden = new[] { "Queue", "Exchange", "Subscription" };

    var declared = iface.GetMethods().Select(m => m.Name).ToHashSet(StringComparer.Ordinal);

    await Assert.That(declared.Intersect(forbidden)).IsEmpty()
        .Because("Per C-003 Kafka does not support queues / exchanges / subscriptions — compile-time error, not runtime throw.");
}
```

---

## CI Strategy

- **Existing CI matrix** already runs per-provider integration jobs. No structural change needed.
- **New matrix entry**: `jetstream-integration` row that mounts the JetStream container and runs `Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/*` only. Skipped when PR diff only touches core-NATS files (Phase 5 task).
- **Coverage gate**: existing Feature-006 coverage gate (`ci.yml` line 363) stays enforcing. No changes required.
- **Parity gate**: no new CI job. The existing `architecture-tests` job already runs `ProviderCompletenessTests`; with C-005 the test reads `.parity-coverage.txt` and enforces progressively.
- **Docs-link check**: existing `ReadmeCompletenessTests` covers `README.md`. Per-provider `docs/providers/*.md` are linked from README so broken links are caught.

---

## Dependency Graph (tasks)

```
                    T000 ─┬─ T001 ─ T002 ─ T003 (PHASE 0, blocks all)
                          │
         ┌────────────────┼────────────────┬────────────────┬────────────────┐
         │                │                │                │                │
       T014              T020             T030             T040             T050
         │                │                │                │                │
    T015a..d RED        T025a/b           T033a/c          T044a/d          T055a/c
    (scenarios lead)     RED               RED              RED              RED
         │                │                │                │                │
    T010 ─ T011 ─       T021 ─ T022 ─     T031 ─ T032       T041 ─ T042      T051 ─ T052 ─
    T012 ─ T013 ─       T023 ─ T024       (flip T033)       T043             T053 ─ T054
    T016                (flip T025)       (flip T033a-c)    (flip T044a-d)   (flip T055a-c)
    (flip T015a-d)
         │                │                │                │                │
         └──────── .parity-coverage.txt appended ──────────┴────────────────┘
                                                                              │
                                                              T060 / T061 / T062 / T063 (PHASE 6)
```

Phases 1–5 are parallel-eligible after Phase 0. Serial recommended for smaller teams to keep the PR queue tractable.

---

## PR Cadence & Reviewer Rules

| PR | Scope | Mandatory reviewer checks |
|----|-------|----------------------------|
| #007-p0 | Phase 0 only | Coverage on new types = 100 %; empty `.parity-coverage.txt` lands; no provider code touched. |
| #007-p1..5 | One per provider | RED scenario commits precede GREEN production commits; `.parity-coverage.txt` appended in exactly one GREEN commit per PR; compile-fence test present; coverage ≥ 90 line / ≥ 85 branch on affected packages; inline XML on every new public type; per-provider doc updated in the same PR. |
| #007-docs-nX | Phase 6 slice per release | `CHANGELOG.md` has exactly one new entry (not batched); `docs/ordering-assertions.md` updated only when the release changes the capability matrix. |

Every PR in this feature MUST:
- Use the branch `feat/007-messaging-topology-sessions`.
- Have zero `--no-verify` / `--no-gpg-sign` commits.
- Have RED commit(s) strictly before GREEN commit(s) per task.
- Never amend across a RED/GREEN boundary.
- Include Codecov diff showing every new public type at 100 % line coverage.

---

## Next

1. `/dotnet-ai-kit:tasks` to break Phase-level tasks into an ordered executable task list with explicit file paths and commit prefixes.
2. `/dotnet-ai-kit:analyze` (optional) to verify spec ↔ plan ↔ task consistency before implementation.
3. `/dotnet-ai-kit:implement` per phase (Phase 0 first).
