# Rig.TUnit.Messaging.Kafka

> Testcontainers-backed Apache Kafka fixture with `KafkaListener` / `KafkaEventSender` and partition-ordered `OrderingAssert`.

## What this package is

The Rig.TUnit Kafka provider. `KafkaFixture` spins
`confluentinc/cp-kafka` in single-broker KRaft mode (no separate
Zookeeper since Kafka 3.3). `KafkaListener` wraps a `Confluent.Kafka`
consumer, subscribes to a topic and records every delivered message
for later assertion. `KafkaEventSender` delegates to the family
`EventSenderBase` so correlation/causation/traceparent headers are
auto-injected.

## When to use it

- Integration tests for Kafka consumers / producers.
- Asserting partition ordering is preserved across a producer / consumer
  pair.
- Verifying consumer-group rebalance behaviour (two listeners in the
  same group).
- **Not for**: pure unit tests of message-handler logic — mock the
  consumer there.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Kafka image ~500 MB)
- `Confluent.Kafka` (transitive)

## Quick start

```csharp
using Rig.TUnit.Messaging.Kafka.Fixtures;
using Rig.TUnit.Messaging.Kafka.Senders;

await using var fx = new KafkaFixture();
await fx.InitializeAsync();

await using var sender = new KafkaEventSender(fx.ConnectionString, topic: "orders");
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"confluentinc/cp-kafka:7.6.1"` | Broker image |
| `StartupTimeoutSeconds` | `int` | `120` | Kafka boot |
| `NumPartitions` | `int` | `3` | Default topic partition count |
| `ReplicationFactor` | `short` | `1` | Single-broker dev cluster |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.Kafka.Fixtures.KafkaFixture`
- `Rig.TUnit.Messaging.Kafka.Options.KafkaFixtureOptions`
- `Rig.TUnit.Messaging.Kafka.Builder.KafkaRigBuilder`
- `Rig.TUnit.Messaging.Kafka.Listeners.KafkaListener`
- `Rig.TUnit.Messaging.Kafka.Senders.KafkaEventSender`

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
  explicitly *not* guaranteed.
- Tombstones (null value) are required to delete keys in log-compacted
  topics — the test harness surfaces this via
  `KafkaEventSender.SendTombstoneAsync`.
- `auto.offset.reset=earliest` is set by default so new consumer groups
  see historical messages; switch to `latest` for replay-free tests.

## Benchmarks

See [`KafkaMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/KafkaMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
