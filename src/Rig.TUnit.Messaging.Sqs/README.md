# Rig.TUnit.Messaging.Sqs

> LocalStack-backed Amazon SQS fixture with FIFO + standard queue topology, `MessageGroupId`-aware listener, and `OrderingAssert` for per-group strict ordering.

## What this package is

The Rig.TUnit Amazon SQS provider. `SqsFixture` spins the LocalStack
image with the SQS feature enabled and returns an `IAmazonSQS` pointing
at it. Ships async listener / sender helpers on `AWSSDK.SQS`. Ships:

- **`SqsEventSender`** — sender with a `SendContext` overload mapping
  `SessionKey` → `MessageGroupId` (mandatory on FIFO queues),
  `DeduplicationKey` → `MessageDeduplicationId`. Throws
  `InvalidOperationException` on FIFO queues when `SessionKey` is missing
  — emulating the broker's precondition before round-trip.
- **`SqsListener`** — requests `MessageGroupId` + `SequenceNumber`
  attributes on receive and populates `CapturedMessage.SessionKey`.
- **`SqsTopologyBuilder`** + provider-scoped interfaces
  (`ISqsTopologyBuilder`, `ISqsQueueConfig`) wired via
  `SqsRigBuilder.WithTopology(…)`. `WithFifo(contentBasedDeduplication)`
  appends the mandatory `.fifo` suffix and writes
  `FifoQueue=true`; `WithDeadLetter(name, maxReceiveCount)` provisions
  the redrive policy; `WithVisibilityTimeout` /
  `WithMessageRetentionPeriod` map to the matching queue attributes.
- **`OrderingAssert`** — knows the FIFO-queue contract (per-
  `MessageGroupId` strict ordering, otherwise best-effort).

## When to use it

- Integration tests for SQS consumers / producers.
- Asserting strict FIFO ordering by `MessageGroupId`.
- Provisioning FIFO queues + DLQs + redrive policies at runtime from
  test code.
- Content-based deduplication verification.
- Dead-letter queue and retry-policy verification.
- **Not for**: unit tests; mock the SQS client.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (LocalStack image ~400 MB)
- `AWSSDK.SQS` (transitive)

## Quick start

```csharp
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Sqs.Fixtures;
using Rig.TUnit.Messaging.Sqs.Helpers;

await using var fx = new SqsFixture();
await fx.InitializeAsync();

var queue = await fx.Client.CreateQueueAsync("orders");
await using var sender = new SqsEventSender(fx.Client, queue.QueueUrl);

await sender.SendAsync(
    "{\"orderId\":1}",
    context: new SendContext(SessionKey: "customer-42"),
    ct: ct);
```

FIFO queue + DLQ via the `WithTopology` rig hook (the `.fifo` suffix is
appended automatically by `WithFifo`):

```csharp
services.AddRigTUnit(rig =>
    rig.UseSqs(RigConnect.FromValue(fx.Endpoint), s =>
        s.WithTopology(t =>
            t.Queue("orders-dlq")
             .Queue("orders", c => c
                 .WithFifo(contentBasedDeduplication: true)
                 .WithDeadLetter("orders-dlq", maxReceiveCount: 5)
                 .WithVisibilityTimeout(TimeSpan.FromSeconds(30))))));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"localstack/localstack:3"` | LocalStack image |
| `StartupTimeoutSeconds` | `int` | `120` | LocalStack boot |
| `Region` | `string` | `"us-east-1"` | Region label |
| `AccessKeyId` | `string` | `"test"` | LocalStack default |
| `SecretAccessKey` | `string` | `"test"` | LocalStack default |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.Sqs.Fixtures.SqsFixture`
- `Rig.TUnit.Messaging.Sqs.Options.SqsFixtureOptions`
- `Rig.TUnit.Messaging.Sqs.Builder.SqsRigBuilder` — ships
  `WithTopology(Action<ISqsTopologyBuilder>)`.
- `Rig.TUnit.Messaging.Sqs.Helpers.SqsListener` — populates
  `CapturedMessage.SessionKey` from `MessageGroupId`.
- `Rig.TUnit.Messaging.Sqs.Helpers.SqsEventSender` — ships
  `SendAsync(string, SendContext, …)` overload.
- `Rig.TUnit.Messaging.Sqs.Topology.ISqsTopologyBuilder`
- `Rig.TUnit.Messaging.Sqs.Topology.ISqsQueueConfig` — `WithFifo`,
  `WithVisibilityTimeout`, `WithDeadLetter`,
  `WithMessageRetentionPeriod`.
- `Rig.TUnit.Messaging.Sqs.Topology.SqsTopologyBuilder`

## Per-test isolation

Per-test queue name: `orders_{IsolationKey:short}.fifo` or `.std`.
LocalStack supports queue create/delete at high concurrency, so
parallelism is effectively unbounded.

## Parallelism + performance

- First-run pull: ~30 s.
- Warm startup: ~10 s.
- Per-op send: ~30–50 ms (LocalStack's SQS endpoint is slower than
  ephemeral-queue real AWS due to per-op HTTP overhead).
- Parallelism: 8+ concurrent tests.

## Troubleshooting

- **Long receive latency** — SQS long-polling defaults to 20 s. Tests
  asserting rapid delivery must set `WaitTimeSeconds=0` on the receive
  request.
- **Message never arrives in FIFO queue** — FIFO queues require
  `MessageGroupId` on send; `SqsEventSender` sets it from the
  `IsolationKey` by default, overridable.

See [docs/troubleshooting.md#sqs](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- FIFO queue names must end with `.fifo`; standard queues must not.
  `ISqsQueueConfig.WithFifo()` appends the suffix automatically — do
  not pass `"orders.fifo"` to `Queue("orders", c => c.WithFifo())`.
- FIFO queues require `MessageGroupId` on every send;
  `SqsEventSender.SendAsync(SendContext)` enforces this and throws
  `InvalidOperationException` before the broker round-trip when
  `SessionKey` is missing on a `.fifo` queue.
- Content-based deduplication requires `ContentBasedDeduplication=true`
  on queue creation — pass `WithFifo(contentBasedDeduplication: true)`.
- `SendContext.PartitionKey` is meaningless on SQS (no partitioned
  primitive); the sender ignores it on standard and FIFO queues.
- Topology apply is idempotent: re-running the same `WithTopology`
  declaration succeeds via `CreateQueueAsync` returning the existing
  queue URL when attributes match.
- LocalStack's SQS diverges from real AWS on: delay-queue timing
  accuracy, cross-region replication, KMS-encrypted queues. Verify
  these against real AWS before prod.

## Benchmarks

See [`SqsMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SqsMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Provider deep-dive: [`docs/providers/sqs.md`](../../docs/providers/sqs.md)
  (FIFO ordering, DLQ redrive, content-based dedup, isolation key
  prefix guidance).
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)
- Feature design: [Sessions & Partitions](../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) ·
  [Topology Builder](../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md)

## License

MIT. See [LICENSE](../../LICENSE).
