# Rig.TUnit.Messaging.ServiceBus

> Microsoft-official Azure ServiceBus emulator fixture (`servicebus-emulator` + SQL Edge sidecar) with native session FIFO listener and runtime topology administration.

## What this package is

The Rig.TUnit Azure ServiceBus provider. `ServiceBusFixture` orchestrates
Microsoft's official ServiceBus emulator
(`mcr.microsoft.com/azure-messaging/servicebus-emulator`) plus the
required SQL Edge sidecar (per C-001 — emulator uses SQL for internal
state). EULA acceptance is mandatory: `options.AcceptEula` must be set
to `true` explicitly. Ships:

- **`ServiceBusEventSender`** — sender with `SendContext` overload
  mapping `SessionKey` → `SessionId`, `PartitionKey` → `PartitionKey`
  (with equality validation), `DeduplicationKey` → `MessageId`.
- **`ServiceBusListener`** — non-session subscription processor.
- **`ServiceBusSessionListener`** — session-aware processor on top of
  `ServiceBusClient.CreateSessionProcessor`; populates
  `CapturedMessage.SessionKey` from the broker's `SessionId` and
  surfaces broker errors via `ObservedErrors` / `LastError`.
- **`ServiceBusAdministrationHelper`** — wraps
  `ServiceBusAdministrationClient` (≥ 7.20.1) with idempotent
  create-or-update operations for topics, subscriptions (with
  `RequiresSession`, `MaxDeliveryCount`, `LockDuration`), SQL rule
  filters, and queues.
- **`ServiceBusTopologyBuilder`** + provider-scoped interfaces
  (`IServiceBusTopologyBuilder`, `IServiceBusTopicConfig`,
  `IServiceBusSubscriptionConfig`, `IServiceBusQueueConfig`) wired to
  the rig via `ServiceBusRigBuilder.WithTopology(…)`.
- **`ServiceBusDeadLetterProbe`** — DLQ inspection helper for tests
  that assert poison-message routing.

## When to use it

- Integration tests for Azure Service Bus topics, subscriptions, and queues.
- Asserting session-ordered delivery (`OrderingAssert.PerKeyMonotonic`
  with `SessionKey`).
- Declaring topology at runtime from test code — no
  `service-bus-config.json` seed required (per Feature 007 T016).
- Verifying SQL rule filter routing (e.g. `Region='EU'`).
- Verifying dead-letter + retry behaviour at `MaxDeliveryCount`.
- **Not for**: unit tests; mock `ServiceBusSender` / `ServiceBusReceiver`.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (emulator + SQL Edge ~1.8 GB combined)
- EULA acceptance (`AcceptEula = true`)
- `Azure.Messaging.ServiceBus` (transitive)

## Quick start

```csharp
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Fixtures;
using Rig.TUnit.Messaging.ServiceBus.Helpers;
using Rig.TUnit.Messaging.ServiceBus.Topology;

// 1) Spin the emulator + SQL sidecar.
await using var fx = new ServiceBusFixture(o => o.AcceptEula = true);
await fx.InitializeAsync();

// 2) Provision topology at runtime via the admin client (no JSON seed).
var admin   = new ServiceBusAdministrationClient(fx.ConnectionString);
var helper  = new ServiceBusAdministrationHelper(admin);
await helper.CreateSubscriptionIfNotExistsAsync(
    "orders", "shipping", requiresSession: true, ct);

// 3) Send + receive with native session FIFO.
await using var client   = new ServiceBusClient(fx.ConnectionString);
await using var sender   = new ServiceBusEventSender(client, "orders");
await using var listener = new ServiceBusSessionListener(client, "orders", "shipping");

await listener.StartAsync(ct);
await sender.SendAsync(
    "{\"orderId\":1}",
    context: new SendContext(SessionKey: "customer-42"),
    ct: ct);
```

Equivalent with the `WithTopology` rig hook:

```csharp
services.AddRigTUnit(rig =>
    rig.UseServiceBus(RigConnect.FromValue(cs), sb =>
        sb.WithTopology(t =>
            t.Topic("orders")
             .Subscription("orders", "shipping", s => s.WithRequiresSession())
             .Subscription("orders", "billing"))));
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `EmulatorImage` | `string` | `"mcr.microsoft.com/azure-messaging/servicebus-emulator:latest"` | Emulator image |
| `SqlEdgeImage` | `string` | `"mcr.microsoft.com/azure-sql-edge:latest"` | SQL sidecar |
| `StartupTimeoutSeconds` | `int` | `300` | Both containers boot |
| `AcceptEula` | `bool` | `false` | MUST be `true` explicitly |
| `SqlSaPassword` | `string` | `"RigTUnit_P@ss1"` | SQL Edge SA |

## Fixture + helper APIs

- `Rig.TUnit.Messaging.ServiceBus.Fixtures.ServiceBusFixture`
- `Rig.TUnit.Messaging.ServiceBus.Options.ServiceBusFixtureOptions`
- `Rig.TUnit.Messaging.ServiceBus.Builder.ServiceBusRigBuilder` —
  ships `WithTopology(Action<IServiceBusTopologyBuilder>)`.
- `Rig.TUnit.Messaging.ServiceBus.Helpers.ServiceBusListener`
- `Rig.TUnit.Messaging.ServiceBus.Helpers.ServiceBusSessionListener` —
  exposes `ObservedSessions`, `ObservedErrors`, `LastError`.
- `Rig.TUnit.Messaging.ServiceBus.Helpers.ServiceBusEventSender` —
  ships `SendAsync(string, SendContext, …)` overload.
- `Rig.TUnit.Messaging.ServiceBus.Topology.IServiceBusTopologyBuilder`
- `Rig.TUnit.Messaging.ServiceBus.Topology.IServiceBusTopicConfig` ·
  `IServiceBusSubscriptionConfig` · `IServiceBusQueueConfig`
- `Rig.TUnit.Messaging.ServiceBus.Topology.ServiceBusTopologyBuilder`
- `Rig.TUnit.Messaging.ServiceBus.Topology.ServiceBusAdministrationHelper`
- `Rig.TUnit.Messaging.ServiceBus.Helpers.ServiceBusDeadLetterProbe`

## Per-test isolation

Per-test topic + subscription named with `{IsolationKey}`. Emulator
config file is regenerated per fixture to declare the per-test topology;
teardown tears down both containers (the emulator's in-SQL state dies
with the SQL container).

## Parallelism + performance

- First-run pull: ~120 s (~1.8 GB combined).
- Warm startup: ~45 s (emulator + SQL init).
- Per-op send: ~5–10 ms.
- Parallelism: 2–4 concurrent tests; the SQL sidecar is the bottleneck.

## Troubleshooting

- **`Failed to accept EULA`** — set `AcceptEula = true`. The failure is
  deliberate and loud so no-one silently accepts commercial terms.
- **`Topic not found`** — emulator config regeneration raced with
  topology assertion; fixture waits for topic existence via the admin
  client before returning.

See [docs/troubleshooting.md#servicebus](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Service Bus sessions preserve FIFO *within* a session; cross-session
  ordering is best-effort. Tests must declare session IDs explicitly via
  `SendContext.SessionKey` and assert with
  `OrderingAssert.PerKeyMonotonic(listener, m => m.SessionKey, …)`.
- `SendContext.SessionKey` and `SendContext.PartitionKey` must be equal
  if both are supplied on a session-aware entity — `ServiceBusEventSender`
  throws `InvalidOperationException` before the broker round-trip.
- `SendContext.DeduplicationKey` requires `RequiresDuplicateDetection`
  on the entity; the topology builder exposes
  `IServiceBusQueueConfig.WithDuplicateDetection(TimeSpan)` /
  `IServiceBusSubscriptionConfig.WithDuplicateDetection(TimeSpan)`.
- Topology is applied idempotently (`CreateOrUpdate*`), so re-running
  the same `WithTopology` declaration in a parallel test is safe.
- Emulator capability gaps (probed by
  `ServiceBusEmulatorCapabilityProbeTests`) are documented in
  [`docs/providers/service-bus.md`](../../docs/providers/service-bus.md).
  Tests that need un-emulated features (advanced SQL filter types,
  auto-forwarding, dead-letter TTL semantics, partitioned-entity
  reconfiguration, premium-tier > 256 KB messages) must run against
  real Azure and use `[Skip]` on the emulator path per C-004.

## Benchmarks

See [`ServiceBusMessagingBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ServiceBusMessagingBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Provider deep-dive: [`docs/providers/service-bus.md`](../../docs/providers/service-bus.md)
  (session FIFO, `WithTopology`, emulator capability table, JSON-seed
  migration).
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)
- Feature design: [Sessions & Partitions](../../planning/messaging-topology-and-sessions/Sessions-And-Partitions-Design.md) ·
  [Topology Builder](../../planning/messaging-topology-and-sessions/Topology-Builder-Design.md)

## License

MIT. See [LICENSE](../../LICENSE).
