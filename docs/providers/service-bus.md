# Azure Service Bus provider

> Docs in progress — populated as Feature 007 phases land.

## Quick-start

```csharp
await using var fixture = new ServiceBusFixture();
await fixture.InitializeAsync();

await using var client = new ServiceBusClient(fixture.ConnectionString);
await using var sender = new ServiceBusEventSender(client, "orders");
await using var listener = new ServiceBusListener(client, "orders", "shipping");

await listener.StartAsync(ct);
await sender.SendAsync("{ \"orderId\": 1 }", correlationId: "c1", ct: ct);
// ...wait, then assert listener.Captured
await listener.StopAsync(ct);
```

## Session FIFO ordering

> Available after Feature 007 Phase 1 (T011-GREEN).

Use `ServiceBusSessionListener` with `SendContext.SessionKey` to route messages through the
broker's native session processor. Messages with the same `SessionKey` are processed in order,
in strict FIFO, within a single session handler.

```csharp
await using var sender = new ServiceBusEventSender(client, "orders");
await using var listener = new ServiceBusSessionListener(client, "orders", "shipping-sessions");

await listener.StartAsync(ct);

for (var i = 0; i < 20; i++)
{
    await sender.SendAsync($"msg-{i}", context: new SendContext(SessionKey: "customer-42"), ct: ct);
}

// wait for messages...

OrderingAssert.PerKeyMonotonic(listener, m => m.SessionId, m => m.SequenceNumber);
```

## Topology via `WithTopology`

> Available after Feature 007 Phase 1 (T012/T013-GREEN).

```csharp
services.AddRigTUnit(rig =>
    rig.UseServiceBus(RigConnect.FromValue(cs), sb =>
        sb.WithTopology(t =>
            t.Topic("orders")
             .Subscription("orders", "shipping", s => s.WithRequiresSession())
             .Subscription("orders", "billing"))));
```

## Emulator capability table

| Feature | Emulator v1.1.2 | Notes |
|---------|----------------|-------|
| Topics + subscriptions | ✓ | Core feature |
| Session-enabled subscriptions | ✓ | T011 verified |
| Partitioned entities | ? | Probe in T014 |
| SQL rule filters | ? | Probe in T014 |
| DLQ (max delivery count) | ✓ | Verified in existing tests |
| Content-based deduplication | ? | Probe in T014 |

_The table above is updated by the emulator capability probe in T014._

## Benchmarks

Allocation benchmarks for session vs non-session message construction are in
`tests/Rig.TUnit.Benchmarks/ServiceBusMessagingBenchmarks.cs`:

| Benchmark | What it measures |
|-----------|-----------------|
| `SessionProcessor_VsNonSession_Throughput` | `ServiceBusMessage` allocation + `SendContext` routing overhead with session ID set |
| `SessionProcessor_NoSession_Baseline` | Same message without a session key — baseline comparison |

Run locally: `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --filter "*ServiceBus*" --exporters json`

## Migration from JSON seed

Prior to Feature 007 T016, session and filter subscriptions were pre-provisioned in
`TestInfrastructure/service-bus-config.json`. That approach coupled every test run to a specific
emulator boot state and made the subscriptions invisible to readers of the test files.

T016 removes these pre-provisioned entries. Tests now create their own uniquely-named subscriptions
using `ServiceBusAdministrationHelper` (or `WithTopology`) and delete them after the test run.
This gives each test full control over its topology and avoids cross-test contamination.

```csharp
// Before T016 — relied on a pre-provisioned subscription in config
await using var listener = new ServiceBusSessionListener(client, "test-topic", "session-ordering-subscription");

// After T016 — test owns its subscription lifecycle
var admin = new ServiceBusAdministrationClient(fx.ConnectionString);
var helper = new ServiceBusAdministrationHelper(admin);
var subName = $"sess-{Guid.NewGuid():N}";
await helper.CreateSubscriptionIfNotExistsAsync("test-topic", subName, requiresSession: true, ct);
await using var listener = new ServiceBusSessionListener(client, "test-topic", subName);
// ... test body ...
await admin.DeleteSubscriptionAsync("test-topic", subName, ct);
```
