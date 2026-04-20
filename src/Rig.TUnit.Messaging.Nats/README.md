# Rig.TUnit.Messaging.Nats

> Testcontainers-backed NATS fixture (`nats:2.10-alpine`) with `NatsListener` / `NatsEventSender` on `NATS.Client.Core`.

## What this package is

The Rig.TUnit NATS provider. `NatsFixture` spins the lightweight
`nats:2.10-alpine` server via Testcontainers (no JetStream by default —
enable via options when testing streams). `NatsListener` subscribes to
a subject pattern and records delivered messages; `NatsEventSender`
publishes with automatic header injection (correlation, causation,
W3C `traceparent`).

## When to use it

- Integration tests for NATS core subject/pub-sub messaging.
- Low-latency messaging scenarios (NATS is sub-millisecond even through
  Docker networking).
- **Not for**: JetStream durable-subscription testing unless you enable
  it explicitly — defaults are core pub/sub.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (NATS image ~20 MB)
- `NATS.Client.Core` (transitive)

## Quick start

```csharp
using Rig.TUnit.Messaging.Nats.Fixtures;
using Rig.TUnit.Messaging.Nats.Senders;

await using var fx = new NatsFixture();
await fx.InitializeAsync();

await using var sender = new NatsEventSender(fx.ConnectionString, subject: "orders");
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"nats:2.10-alpine"` | NATS image |
| `StartupTimeoutSeconds` | `int` | `30` | NATS boots in ~1 s |
| `EnableJetStream` | `bool` | `false` | Durable streams |
| `JetStreamStorageDir` | `string?` | `null` | JetStream persistence path |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.Nats.Fixtures.NatsFixture`
- `Rig.TUnit.Messaging.Nats.Options.NatsFixtureOptions`
- `Rig.TUnit.Messaging.Nats.Builder.NatsRigBuilder`
- `Rig.TUnit.Messaging.Nats.Listeners.NatsListener`
- `Rig.TUnit.Messaging.Nats.Senders.NatsEventSender`

## Per-test isolation

Per-test subject: `orders.{IsolationKey:short}`. NATS subjects are
namespace-scoped by dot-separated tokens so parallel tests never
collide. No teardown needed — subjects are ephemeral.

## Parallelism + performance

- First-run pull: ~2 s.
- Warm startup: ~1 s.
- Per-op send + receive: ~500 µs.
- Parallelism: essentially unbounded — NATS is designed for it.

## Troubleshooting

- **`No responders` error** — the subscriber wasn't active when the
  publisher fired. Use `listener.StartAsync` + `MessageAssert.Within`
  rather than a fixed sleep.
- **JetStream stream missing** — `EnableJetStream=false` by default;
  turn it on explicitly.

See [docs/troubleshooting.md#nats](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- NATS core is best-effort delivery — a restart loses unacked messages.
  Durability needs JetStream; turn on via `EnableJetStream=true`.
- Subject wildcards (`orders.*`, `orders.>`) match differently —
  `*` is single-token, `>` is multi-token. Tests asserting on wildcard
  routing must be explicit.

## Benchmarks

See [`NatsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/NatsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
