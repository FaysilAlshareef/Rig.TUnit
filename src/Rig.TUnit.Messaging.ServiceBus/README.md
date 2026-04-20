# Rig.TUnit.Messaging.ServiceBus

> Microsoft-official Azure ServiceBus emulator fixture (`servicebus-emulator` + SQL Edge sidecar).

## What this package is

The Rig.TUnit Azure ServiceBus provider. `ServiceBusFixture` orchestrates
Microsoft's official ServiceBus emulator
(`mcr.microsoft.com/azure-messaging/servicebus-emulator`) plus the
required SQL Edge sidecar (per C-001 — emulator uses SQL for internal
state). EULA acceptance is mandatory: options.AcceptEula must be set to
true explicitly. Ships session-aware `ServiceBusListener` /
`ServiceBusEventSender` helpers.

## When to use it

- Integration tests for Azure ServiceBus topics/subscriptions/queues.
- Asserting session-ordered delivery guarantees.
- Verifying dead-letter + retry behaviour.
- **Not for**: unit tests; mock `ServiceBusSender` / `ServiceBusReceiver`.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (emulator + SQL Edge ~1.8 GB combined)
- EULA acceptance (`AcceptEula = true`)
- `Azure.Messaging.ServiceBus` (transitive)

## Quick start

```csharp
using Azure.Messaging.ServiceBus;
using Rig.TUnit.Messaging.ServiceBus.Fixtures;
using Rig.TUnit.Messaging.ServiceBus.Senders;

await using var fx = new ServiceBusFixture();
await fx.InitializeAsync();

await using var sender = new ServiceBusEventSender(
    new ServiceBusClient(fx.ConnectionString), topic: "orders");
await sender.SendAsync("{\"orderId\":1}", correlationId: "abc");
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
- `Rig.TUnit.Messaging.ServiceBus.Builder.ServiceBusRigBuilder`
- `Rig.TUnit.Messaging.ServiceBus.Listeners.ServiceBusListener`
- `Rig.TUnit.Messaging.ServiceBus.Senders.ServiceBusEventSender`

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

- ServiceBus sessions preserve FIFO *within* a session; cross-session
  ordering is best-effort. Tests must declare session IDs explicitly.
- The emulator diverges from production on: advanced filter expressions
  (SQL filter types), auto-forwarding, dead-letter TTL semantics.
  Tests relying on these must run against real Azure.
- Messages over 256 KB require `premium` tier — the emulator enforces
  the 256 KB limit.

## Benchmarks

See [`ServiceBusBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ServiceBusBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Messaging`](../Rig.TUnit.Messaging/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
