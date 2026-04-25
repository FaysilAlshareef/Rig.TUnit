# Provider Enhancement Matrix

**Feature**: 007
**Purpose**: Ground-truth per-provider change list with effort, linked to the roadmap tasks.

---

## Legend

- ✅ Already supported today
- ⚠️ Partial — exists but with limitations
- ❌ Not supported
- 🟢 Low effort (< 4 h)
- 🟡 Medium effort (4–8 h)
- 🔴 High effort (> 8 h, or new dependency)

---

## Sessions / partition key support

| Capability | ServiceBus | Kafka | RabbitMQ | NATS | SQS |
|---|---|---|---|---|---|
| Native session primitive exists | ✅ `SessionId` | ⚠️ (partition key, no session) | ❌ (routing key only) | ⚠️ (JetStream subject only) | ✅ (FIFO `MessageGroupId`) |
| Rig sender sets session/partition key | ❌ | ⚠️ (conflated with `correlationId`) | ❌ | ❌ | ❌ |
| Rig listener recovers session/partition key into `CapturedMessage` | ❌ | ❌ | ❌ | ❌ | ❌ |
| Session-aware listener (ordered consumer) | ❌ | ✅ (per-partition) | ❌ | ❌ (core NATS) | ✅ (per group) |
| Effort this feature | 🟡 | 🟢 | 🟡 | 🔴 | 🟢 |
| Roadmap tasks | T010, T011, T015 | T020, T024, T025 | T040 (via routing key), T041 | T051, T052, T053 | T030, T032, T033 |

---

## Topology creation

| Capability | ServiceBus | Kafka | RabbitMQ | NATS | SQS |
|---|---|---|---|---|---|
| SDK admin client exists | ✅ `ServiceBusAdministrationClient` (v7.20.1+) | ✅ `IAdminClient` | ✅ `IChannel` | ✅ `INatsJSContext` | ✅ `IAmazonSQS` |
| Rig uses SDK today | ❌ (JSON seed file) | ⚠️ (topic only, hardcoded 1 partition) | ⚠️ (queue only) | ❌ | ❌ |
| Rig exposes fluent `WithTopology` | ❌ | ❌ | ❌ | ❌ | ❌ |
| Supports DLQ configuration | ❌ | n/a | ❌ | ❌ | ❌ |
| Supports filters / rules | ❌ (SQL filter not exposed) | ❌ (no configs) | ❌ | ❌ | ❌ |
| Effort this feature | 🔴 | 🟡 | 🔴 | 🔴 (new dep) | 🟡 |
| Roadmap tasks | T012, T013, T016 | T022, T023 | T042, T043 | T054 | T031 |

---

## Per-provider detailed change list

### ServiceBus — largest topology change, medium sessions change

| Change | File | Effort | Task |
|---|---|---|---|
| `sessionId` + `partitionKey` params on sender + precondition validation | `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusEventSender.cs` | 🟢 | T010 |
| `ServiceBusSessionListener` using `CreateSessionProcessor` | `src/Rig.TUnit.Messaging.ServiceBus/Helpers/ServiceBusSessionListener.cs` (new) | 🟡 | T011 |
| `ServiceBusAdministrationHelper` — create topic/subscription/rule/DLQ | `src/Rig.TUnit.Messaging.ServiceBus/Topology/` (new) | 🔴 | T012 |
| `ServiceBusRigBuilder.WithTopology` hook | `src/Rig.TUnit.Messaging.ServiceBus/Builder/ServiceBusRigBuilder.cs` | 🟢 | T013 |
| Bump package version | `Directory.Packages.props` | 🟢 | T014 |
| 4 integration scenarios: session FIFO, partitioned fan-out, DLQ, SQL filter | `tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Sessions/` | 🟡 | T015 |
| Shrink seed JSON to namespace only | `TestInfrastructure/service-bus-config.json` | 🟢 | T016 |

**Total**: ~3 days. Biggest unknown: emulator coverage of admin-client operations.

### Kafka — smallest change, biggest parity win

| Change | File | Effort | Task |
|---|---|---|---|
| Decouple `PartitionKey` from `correlationId` on sender | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaEventSender.cs` | 🟢 | T020 |
| `DefaultPartitions` option (default 1) | `src/Rig.TUnit.Messaging.Kafka/Options/KafkaFixtureOptions.cs` | 🟢 | T021 |
| Topic creation honours partitions + configs | `src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs` | 🟢 | T022 |
| `KafkaTopologyBuilder` + `WithTopic` fluent | `src/Rig.TUnit.Messaging.Kafka/Topology/` (new) | 🟡 | T023 |
| Manual partition assignment helper (optional) | `KafkaListener.cs` | 🟢 | T024 |
| Multi-partition per-key ordering test | `tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/Partitions/` | 🟡 | T025 |

**Total**: ~1.5 days. Highest ROI of the whole feature.

### RabbitMQ — largest new API surface

| Change | File | Effort | Task |
|---|---|---|---|
| Explicit `exchange` + `routingKey` on sender | `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqEventSender.cs` | 🟢 | T040 |
| Listener supports exchange + binding declaration | `src/Rig.TUnit.Messaging.RabbitMq/Helpers/RabbitMqListener.cs` | 🟢 | T041 |
| `RabbitMqTopologyBuilder` (exchange / queue / binding / DLX) | `src/Rig.TUnit.Messaging.RabbitMq/Topology/` (new) | 🔴 | T042 |
| Queue-argument plumbing: TTL, DLX, max-length, priority, quorum | T042 cont. | 🟢 | T043 |
| 4 integration scenarios: topic exchange, DLX, priority, quorum | `tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/Topology/` | 🟡 | T044 |

**Total**: ~2 days.

### NATS — new JetStream fixture (largest investment)

| Change | File | Effort | Task |
|---|---|---|---|
| `NATS.Client.JetStream` dependency | `Directory.Packages.props`, `.csproj` | 🟢 | T050 |
| `NatsJetStreamFixture` | `src/Rig.TUnit.Messaging.Nats/Fixtures/NatsJetStreamFixture.cs` (new) | 🟡 | T051 |
| `NatsJetStreamEventSender` | `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamEventSender.cs` (new) | 🟡 | T052 |
| `NatsJetStreamListener` (ordered consumer) | `src/Rig.TUnit.Messaging.Nats/Helpers/NatsJetStreamListener.cs` (new) | 🟡 | T053 |
| `NatsTopologyBuilder` (stream + consumer) | `src/Rig.TUnit.Messaging.Nats/Topology/` (new) | 🟡 | T054 |
| 3 integration scenarios: ordered across reconnect, multi-subject, retention | `tests/Rig.TUnit.Messaging.Nats.Tests.Integration/JetStream/` | 🟡 | T055 |

**Total**: ~2.5 days. Keeps the existing core-NATS fixture untouched and green.

### SQS — straightforward FIFO path

| Change | File | Effort | Task |
|---|---|---|---|
| `messageGroupId` + `messageDeduplicationId` on sender | `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsEventSender.cs` | 🟢 | T030 |
| `SqsTopologyBuilder` — FIFO/standard + DLQ | `src/Rig.TUnit.Messaging.Sqs/Topology/` (new) | 🟡 | T031 |
| Listener requests `MessageGroupId` / `SequenceNumber` attributes | `src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs` | 🟢 | T032 |
| FIFO ordering + DLQ redrive + content dedup tests | `tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/Fifo/` | 🟡 | T033 |

**Total**: ~1.5 days.

---

## Aggregate effort

| Provider | Sessions | Topology | Tests + Docs | Total |
|---|---|---|---|---|
| ServiceBus | 🟡 | 🔴 | 🟡 | **~3 d** |
| Kafka | 🟢 | 🟡 | 🟡 | **~1.5 d** |
| RabbitMQ | 🟢 | 🔴 | 🟡 | **~2 d** |
| NATS | 🔴 | 🔴 | 🟡 | **~2.5 d** |
| SQS | 🟢 | 🟡 | 🟡 | **~1.5 d** |
| Base library (Phase 0) | — | — | — | **~1 d** |
| Docs & benchmarks (Phase 6) | — | — | — | **~1 d** |
| **Serial total** | | | | **~12.5 d** |
| **Parallel (2 devs)** | | | | **~7 d** |

---

## Dependency graph

```
Phase 0 (base library)
    ├── Phase 1 (ServiceBus — primary ask)
    ├── Phase 2 (Kafka)
    ├── Phase 3 (SQS)
    ├── Phase 4 (RabbitMQ)
    └── Phase 5 (NATS JetStream)
             └── Phase 6 (docs + benchmarks, after all providers)
```

Phases 1–5 can run in parallel after Phase 0 completes. Phase 6 is the single serial
dependency at the end.
