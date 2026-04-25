# Kafka provider

> Docs in progress — populated as Feature 007 phases land.

## Quick-start

```csharp
await using var fixture = new SharedKafkaFixture();
await using var sender = new KafkaEventSender(fixture.ConnectionString, "orders");
var listener = new KafkaListener(fixture.ConnectionString, "orders", "shipping-group");

await listener.StartAsync(ct);
await sender.SendAsync("{ \"orderId\": 1 }", ct: ct);
// ...wait, then assert listener.Count
await listener.StopAsync(ct);
```

## Partition-key routing

> Available after Feature 007 Phase 2 (T020-GREEN).

Use `SendContext.PartitionKey` to route messages to a deterministic partition. Kafka's default
partitioner hashes the key so all messages with the same key land on the same partition —
preserving per-key ordering.

```csharp
await using var sender = new KafkaEventSender(fixture.ConnectionString, "orders");
var listener = new KafkaListener(fixture.ConnectionString, "orders", "shipping-group");

await listener.StartAsync(ct);

for (var i = 0; i < 20; i++)
{
    await sender.SendAsync($"msg-{i}", context: new SendContext(PartitionKey: "customer-42"), ct: ct);
}

// wait for messages...

OrderingAssert.PerKeyMonotonic(listener, m => m.Message.Key, m => m.Offset.Value);
```

## Topology via `WithTopology`

> Available after Feature 007 Phase 2 (T023-GREEN).

```csharp
services.AddRigTUnit(rig =>
    rig.UseKafka(RigConnect.FromValue(cs), kafka =>
        kafka.WithTopology(t =>
            t.Topic("orders", cfg => cfg
                .WithPartitions(6)
                .WithReplicationFactor(1)
                .WithConfig("cleanup.policy", "compact")))));
```

## Benchmarks

Allocation benchmarks for multi-partition per-key fan-out are in
`tests/Rig.TUnit.Benchmarks/KafkaMessagingBenchmarks.cs`:

| Benchmark | What it measures |
|-----------|-----------------|
| `MultiPartition_PerKey_Throughput` | Allocation burst of constructing 8 `SendContext` values each with a distinct `PartitionKey` — models the per-key routing overhead at fan-out start |

Run locally: `dotnet run -c Release --project tests/Rig.TUnit.Benchmarks -- --filter "*Kafka*" --exporters json`

## Compacted topics

Set `cleanup.policy=compact` via `WithTopology` (or directly via Kafka Admin API) to ensure only
the latest value per key is retained after log compaction runs.

```csharp
builder.WithTopology(t =>
    t.Topic("order-snapshots", cfg => cfg
        .WithPartitions(1)
        .WithConfig("cleanup.policy", "compact")));
await builder.ApplyTopologyAsync(ct);
```
