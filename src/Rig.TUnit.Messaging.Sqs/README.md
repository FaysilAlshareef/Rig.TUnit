# Rig.TUnit.Messaging.Sqs

> LocalStack-backed Amazon SQS fixture with `SqsListener` / `SqsEventSender` and FIFO-ordering assertions.

## What this package is

The Rig.TUnit Amazon SQS provider. `SqsFixture` spins the LocalStack
image with the SQS feature enabled and returns an `IAmazonSQS` pointing
at it. Ships async listener / sender helpers on `AWSSDK.SQS`.
`OrderingAssert` knows the FIFO-queue contract — for `.fifo` queues,
per-`MessageGroupId` ordering is strict; otherwise best-effort.

## When to use it

- Integration tests for SQS consumers/producers.
- Asserting FIFO ordering by `MessageGroupId`.
- Dead-letter queue and retry-policy verification.
- **Not for**: unit tests; mock the SQS client.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (LocalStack image ~400 MB)
- `AWSSDK.SQS` (transitive)

## Quick start

```csharp
using Rig.TUnit.Messaging.Sqs.Fixtures;
using Rig.TUnit.Messaging.Sqs.Senders;

await using var fx = new SqsFixture();
await fx.InitializeAsync();

var queue = await fx.Client.CreateQueueAsync("orders");
await using var sender = new SqsEventSender(fx.Client, queue.QueueUrl);
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
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
- `Rig.TUnit.Messaging.Sqs.Builder.SqsRigBuilder`
- `Rig.TUnit.Messaging.Sqs.Listeners.SqsListener`
- `Rig.TUnit.Messaging.Sqs.Senders.SqsEventSender`

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
- Content-based deduplication requires `ContentBasedDeduplication=true`
  on queue creation.
- LocalStack's SQS diverges from real AWS on: delay-queue timing
  accuracy, cross-region replication, KMS-encrypted queues. Verify
  these against real AWS before prod.

## Benchmarks

See [`SqsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/SqsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
