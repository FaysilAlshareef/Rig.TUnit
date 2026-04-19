# Rig.TUnit.Databases.NoSql.KurrentDb

Testcontainers-backed **KurrentDB** (the post-rebrand Event Store — see https://www.kurrent.io/blog/kurrent-re-brand-faq) provider. Ships `KurrentDbFixture`, `KurrentDbFixtureOptions`, `KurrentDbRigBuilder`, the `UseKurrentDb(source, cfg => ...)` fluent entry on `RigBuilder`, and `StreamAssert.EventsAppendedAsync` — reads a stream forwards from the start and returns the total event count (missing streams return 0).

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.KurrentDb
```

## Example

```csharp
public sealed class OrderStreamTests
{
    private readonly KurrentDbFixture _es = new();

    [Before(Test)] public Task Init() => _es.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _es.DisposeAsync();

    [Test]
    public async Task OrderPlaced_ThreeEvents_Appended()
    {
        var stream = $"order-{Guid.NewGuid():N}";
        await _es.Client.AppendToStreamAsync(
            stream,
            StreamState.NoStream,
            new[]
            {
                new EventData(Uuid.NewUuid(), "order-placed", payload: "{}"u8.ToArray()),
                new EventData(Uuid.NewUuid(), "line-added",  payload: "{}"u8.ToArray()),
                new EventData(Uuid.NewUuid(), "order-paid",  payload: "{}"u8.ToArray()),
            });

        var count = await StreamAssert.EventsAppendedAsync(_es.Client, stream);
        await Assert.That(count).IsEqualTo(3L);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.NoSql`, `Testcontainers.KurrentDb` (4.11+), `KurrentDB.Client` (1.3+).

> **Note.** The package name tracks the upstream rebrand — `EventStoreDb` became `KurrentDb` in Testcontainers 4.9 and `EventStore.Client.Grpc.Streams` became `KurrentDB.Client` in 1.x.
