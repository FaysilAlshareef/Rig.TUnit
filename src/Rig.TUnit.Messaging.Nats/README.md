# Rig.TUnit.Messaging.Nats

> Two parallel fixtures: core NATS (`NatsFixture`) for low-latency pub/sub and JetStream (`NatsJetStreamFixture`) for durable streams + ordered consumers, with a fluent topology builder.

## What this package is

The Rig.TUnit NATS provider. Two fixture flavours, both Testcontainers-backed:

- **`NatsFixture`** — `nats:2.10-alpine` server, JetStream off; ships
  `NatsListener` + `NatsEventSender` for core pub/sub testing.
- **`NatsJetStreamFixture`** — same image with `-js` flag, exposes an
  `INatsJSContext`; ships:
  - **`NatsJetStreamEventSender`** — publishes via
    `INatsJSContext.PublishAsync` with a `SendContext` overload that
    writes `x-correlation-id` + `x-session-key` headers.
  - **`NatsJetStreamListener`** — ordered consumer
    (`CreateOrderedConsumerAsync`); supports multi-subject filters and
    survives reconnects without duplicates. Reads `x-session-key` into
    `CapturedMessage.SessionKey`.
  - **`NatsTopologyBuilder`** + provider-scoped interfaces
    (`INatsTopologyBuilder`, `INatsStreamConfig`,
    `INatsConsumerConfig`, `NatsRetentionPolicy` enum) wired via
    `NatsRigBuilder.WithTopology(…)`. Stream creation is idempotent
    via `JSStreamNameExistErr` (10058) → `UpdateStreamAsync` fallback.

The core-NATS API is unchanged from prior releases. JetStream is a
strictly additive layer (Feature 007 Phase 5) and uses an isolated
`NATS.Client.JetStream` package reference, guarded by an architecture
test (`DependencyDirectionTests`).

## When to use it

- **Core NATS** — low-latency pub/sub scenarios (sub-millisecond even
  through Docker networking), best-effort delivery is acceptable.
- **JetStream** — durable streams, ordered consumers, retention
  policies, multi-subject filtering, ack-tracked delivery.
- Asserting per-key ordering on a JetStream subject hierarchy.
- **Not for**: pure unit tests of message-handler logic.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (NATS image ~20 MB)
- `NATS.Client.Core` (transitive)

## Quick start

Core NATS — best-effort pub/sub:

```csharp
using Rig.TUnit.Messaging.Nats.Fixtures;
using Rig.TUnit.Messaging.Nats.Helpers;

await using var fx = new NatsFixture();
await fx.InitializeAsync();

await using var sender = new NatsEventSender(fx.ConnectionString, subject: "orders");
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
```

JetStream — durable + ordered:

```csharp
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Nats.Fixtures;
using Rig.TUnit.Messaging.Nats.Helpers;

var fx = await SharedNatsJetStreamFixture.GetAsync();

// 1) Declare topology at runtime.
services.AddRigTUnit(rig =>
    rig.UseNats(RigConnect.FromValue(fx.ConnectionString), n =>
        n.WithTopology(t => t.Stream("ORDERS", c => c
            .WithSubjects("orders.>")
            .WithMaxMessages(10_000)
            .WithRetentionPolicy(NatsRetentionPolicy.Limits)))));

// 2) Send + ordered receive.
await using var sender   = new NatsJetStreamEventSender(fx.JetStream, "orders.eu");
await using var listener = new NatsJetStreamListener(fx.JetStream, "ORDERS", "orders.eu");

await listener.StartAsync(ct);
await sender.SendAsync(
    "payload-1",
    context: new SendContext(SessionKey: "session-abc"),
    ct: ct);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"nats:2.10-alpine"` | NATS image |
| `StartupTimeoutSeconds` | `int` | `30` | NATS boots in ~1 s |
| `EnableJetStream` | `bool` | `false` | Durable streams |
| `JetStreamStorageDir` | `string?` | `null` | JetStream persistence path |

## Fixture + helper APIs

Core NATS:

- `Rig.TUnit.Messaging.Nats.Fixtures.NatsFixture`
- `Rig.TUnit.Messaging.Nats.Options.NatsFixtureOptions`
- `Rig.TUnit.Messaging.Nats.Builder.NatsRigBuilder` — ships
  `WithTopology(Action<INatsTopologyBuilder>)`.
- `Rig.TUnit.Messaging.Nats.Helpers.NatsListener`
- `Rig.TUnit.Messaging.Nats.Helpers.NatsEventSender`

JetStream (Feature 007 Phase 5):

- `Rig.TUnit.Messaging.Nats.Fixtures.NatsJetStreamFixture` ·
  `SharedNatsJetStreamFixture`
- `Rig.TUnit.Messaging.Nats.Helpers.NatsJetStreamEventSender` — ships
  `SendAsync(string, SendContext, …)` overload.
- `Rig.TUnit.Messaging.Nats.Helpers.NatsJetStreamListener` —
  ordered consumer with optional multi-subject filters.
- `Rig.TUnit.Messaging.Nats.Topology.INatsTopologyBuilder`
- `Rig.TUnit.Messaging.Nats.Topology.INatsStreamConfig` —
  `WithSubjects`, `WithMaxMessages`, `WithRetentionPolicy`.
- `Rig.TUnit.Messaging.Nats.Topology.INatsConsumerConfig`
- `Rig.TUnit.Messaging.Nats.Topology.NatsRetentionPolicy` — enum
  (`Limits`, `Interest`, `WorkQueue`).
- `Rig.TUnit.Messaging.Nats.Topology.NatsTopologyBuilder`

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
  Durability needs JetStream; use `NatsJetStreamFixture` (separate
  fixture, separate package reference, separate CI matrix entry).
- Subject wildcards (`orders.*`, `orders.>`) match differently —
  `*` is single-token, `>` is multi-token. Tests asserting on wildcard
  routing must be explicit.
- JetStream stream creation is idempotent: the topology builder
  catches `NatsJSApiException.Error.ErrCode == 10058`
  (`JSStreamNameExistErr`) and falls back to `UpdateStreamAsync`. Other
  400-coded errors (e.g. invalid retention policy) propagate so they
  surface in tests.
- `NatsJetStreamListener.StartAsync` creates the ordered consumer
  synchronously **before** spawning the consume loop — so a fast
  publisher can never race a not-yet-existing consumer, and any broker
  error (auth, missing stream) propagates to the test.
- `SendContext.PartitionKey` is unused on JetStream — partitioning is
  done via the subject hierarchy. Use `SessionKey` for per-key
  ordering, mapped to the `x-session-key` header.
- The JetStream code path sits behind its own `NATS.Client.JetStream`
  package reference, which `DependencyDirectionTests` enforces is
  referenced **only** by `Rig.TUnit.Messaging.Nats`.

## Benchmarks

See [`NatsMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/NatsMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Provider deep-dive: [`docs/providers/nats.md`](../../docs/providers/nats.md)
  (JetStream stream / consumer examples, retention policies, dependency
  guard).
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)
- Feature design: [Sessions & Partitions](../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) ·
  [Topology Builder](../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md)

## License

MIT. See [LICENSE](../../LICENSE).
