# Rig.TUnit.Messaging.Kafka

> Testcontainers-backed Apache Kafka fixture with `KafkaListener` / `KafkaEventSender`, multi-partition `Confluent.Kafka` admin-driven topology, and partition-ordered `OrderingAssert`.

## What this package is

The Rig.TUnit Kafka provider. `KafkaFixture` spins
`confluentinc/cp-kafka` in single-broker KRaft mode (no separate
Zookeeper since Kafka 3.3). Ships:

- **`KafkaEventSender`** — sender with a `SendContext` overload mapping
  `PartitionKey` (or `SessionKey` as a fallback) to the Kafka
  `Message.Key`. The legacy `correlationId → Message.Key` conflation
  was decoupled in Feature 007 T020.
- **`KafkaListener`** — wraps a `Confluent.Kafka` consumer; `Subscribe`
  for group-managed assignment or `Assign(int partition)` to pin a
  listener to a specific partition. Pre-creates the topic before
  subscribing (so a fast publisher never races a not-yet-existing topic).
- **`KafkaTopologyBuilder`** + `IKafkaTopologyBuilder` /
  `IKafkaTopicConfig` — multi-partition + custom configs (retention,
  cleanup policy) via the `Confluent.Kafka.AdminClient`. Wired to the
  rig via `KafkaRigBuilder.WithTopology(…)`.
- **`KafkaFixtureOptions.DefaultPartitions`** — global default for
  topics created by the listener.

## When to use it

- Integration tests for Kafka consumers / producers.
- Asserting partition ordering is preserved across a producer / consumer
  pair (multi-partition `OrderingAssert.PerKeyMonotonic` per key).
- Verifying consumer-group rebalance behaviour (two listeners in the
  same group).
- Declaring topics with custom partition counts, retention, or
  log-compaction at runtime from test code.
- **Not for**: pure unit tests of message-handler logic — mock the
  consumer there.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Kafka image ~500 MB)
- `Confluent.Kafka` (transitive)

## Quick start

```csharp
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Kafka.Fixtures;
using Rig.TUnit.Messaging.Kafka.Helpers;

await using var fx = new KafkaFixture();
await fx.InitializeAsync();

// Per-key ordered send (hashed to a partition by Confluent.Kafka).
await using var sender   = new KafkaEventSender(fx.ConnectionString, topic: "orders");
await using var listener = new KafkaListener(fx.ConnectionString, "orders", groupId: "shipping");

await listener.StartAsync(ct);
await sender.SendAsync(
    "{\"orderId\":1}",
    context: new SendContext(PartitionKey: "customer-42"),
    ct: ct);
```

Multi-partition topology via the `WithTopology` rig hook:

```csharp
services.AddRigTUnit(rig =>
    rig.UseKafka(RigConnect.FromValue(fx.ConnectionString), k =>
        k.WithTopology(t =>
            t.Topic("orders", c => c
                .WithPartitions(6)
                .WithReplicationFactor(1)
                .WithConfig("cleanup.policy", "compact")
                .WithConfig("retention.ms", "86400000")))));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"confluentinc/cp-kafka:7.6.1"` | Broker image |
| `StartupTimeoutSeconds` | `int` | `120` | Kafka boot |
| `NumPartitions` | `int` | `3` | Default topic partition count |
| `ReplicationFactor` | `short` | `1` | Single-broker dev cluster |
| `DefaultPartitions` | `int` | `1` | Per-listener default when no topology declares the topic (range 1–200) |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.Kafka.Fixtures.KafkaFixture`
- `Rig.TUnit.Messaging.Kafka.Options.KafkaFixtureOptions` —
  `DefaultPartitions` controls the auto-create count.
- `Rig.TUnit.Messaging.Kafka.Builder.KafkaRigBuilder` — ships
  `WithTopology(Action<IKafkaTopologyBuilder>)`.
- `Rig.TUnit.Messaging.Kafka.Helpers.KafkaListener` — `Subscribe` or
  `Assign(int partition)` for pinned-partition tests.
- `Rig.TUnit.Messaging.Kafka.Helpers.KafkaEventSender` — ships
  `SendAsync(string, SendContext, …)` overload.
- `Rig.TUnit.Messaging.Kafka.Topology.IKafkaTopologyBuilder`
- `Rig.TUnit.Messaging.Kafka.Topology.IKafkaTopicConfig` —
  `WithPartitions`, `WithReplicationFactor`, `WithConfig(key, value)`.
- `Rig.TUnit.Messaging.Kafka.Topology.KafkaTopologyBuilder`

## Per-test isolation

Per-test topic `orders_{IsolationKey:short}`. Teardown deletes the
topic. Consumer groups also carry the `IsolationKey` suffix so parallel
tests do not join the same rebalance group.

## Parallelism + performance

- First-run pull: ~60 s.
- Warm startup: ~10–15 s.
- Per-op send: ~3–5 ms.
- **Bind-port contention** — each Kafka broker binds a single host port;
  multiple fixtures run in parallel because Testcontainers allocates
  ephemeral ports, but `KafkaFixtureOptions.FixedHostPort` breaks this.

## Troubleshooting

- **`Local: Broker transport failure`** — consumer started before broker
  finished topic creation. `KafkaListener.StartAsync` retries with
  exponential backoff; do not shortcut the wait.
- **Consumer-group rebalance loops forever** — two listeners use the
  same group id; parallel tests must suffix with `IsolationKey`.
- **Message not received but sent** — producer's delivery report arrived
  but consumer hasn't polled yet; poll with `MessageAssert.Within(…)`
  rather than a fixed delay.

See [docs/troubleshooting.md#kafka](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Kafka's partition-level ordering guarantee is the strongest thing
  asserted in `OrderingAssert`; global ordering across partitions is
  explicitly *not* guaranteed. Use `SendContext.PartitionKey` so all
  messages for a key land in the same partition.
- Kafka has no native session concept; if `SendContext.SessionKey` is
  supplied without `PartitionKey`, the sender folds it to the
  `Message.Key` automatically.
- `correlationId` no longer doubles as `Message.Key` — the legacy
  conflation in `KafkaEventSender` was decoupled. Tests that relied on
  the old behaviour must pass `PartitionKey` explicitly.
- Tombstones (null value) are required to delete keys in log-compacted
  topics — the test harness surfaces this via
  `KafkaEventSender.SendTombstoneAsync`.
- `auto.offset.reset=earliest` is set by default so new consumer groups
  see historical messages; switch to `latest` for replay-free tests.
- Topology apply is idempotent: `EnsureTopicExistsAsync` swallows
  `CreateTopicsException` when every result is `TopicAlreadyExists`.

## Benchmarks

See [`KafkaMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/KafkaMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Provider deep-dive: [`docs/providers/kafka.md`](../../docs/providers/kafka.md)
  (multi-partition ordering, topic configs, pinned-partition examples).
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)
- Feature design: [Sessions & Partitions](../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) ·
  [Topology Builder](../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md)

## License

MIT. See [LICENSE](../../LICENSE).
