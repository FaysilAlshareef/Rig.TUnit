# NATS Provider

Rig.TUnit ships two NATS sub-packages:

- **`NATS.Client.Core`** — core pub/sub (used by `NatsFixture`, `NatsEventSender`, `NatsListener`)
- **`NATS.Client.JetStream`** — durable streams and ordered consumers (used by the JetStream helpers below)

## Dependency note

`NATS.Client.JetStream` is referenced **only** by `Rig.TUnit.Messaging.Nats`. No other provider package may take a dependency on it (enforced by `DependencyDirectionTests.NatsJetStream_ReferencedOnlyByNatsProvider`).

## Quick-start (core pub/sub)

```csharp
var fx = await SharedNatsFixture.GetAsync();
await using var sender   = new NatsEventSender(fx.ConnectionString, "my-subject");
await using var listener = new NatsListener(fx.ConnectionString, "my-subject");
await listener.StartAsync(ct);

await sender.SendAsync("hello", ct: ct);
await MessageAssert.Within(listener, TimeSpan.FromSeconds(5), expectedCount: 1, ct);
```

## JetStream — ordered consumer

```csharp
var fx = await SharedNatsJetStreamFixture.GetAsync();
await using var sender   = new NatsJetStreamEventSender(fx.JetStream, "events.>");
await using var listener = new NatsJetStreamListener(fx.JetStream, "events.>");
await listener.StartAsync(ct);

await sender.SendAsync("payload", new SendContext(SessionKey: "order-1"), ct: ct);
await MessageAssert.Within(listener, TimeSpan.FromSeconds(10), expectedCount: 1, ct);
await Assert.That(listener.Captured.First().SessionKey).IsEqualTo("order-1");
```

## WithTopology

```csharp
new ServiceCollection().AddRigTUnit(rig =>
    rig.UseNats(fx, builder =>
        builder.WithTopology(t =>
            t.Stream("orders", cfg => cfg
                .WithSubjects("orders.>")
                .WithRetentionPolicy(RetentionPolicy.Limits)
                .WithMaxMessages(10_000)))));
```

## Fixture — core vs JetStream

| Fixture | Backed by | When to use |
|---------|-----------|-------------|
| `NatsFixture` | Core NATS pub/sub | Stateless fan-out, fire-and-forget |
| `NatsJetStreamFixture` | JetStream context | Durable consumers, ordering, retention |
