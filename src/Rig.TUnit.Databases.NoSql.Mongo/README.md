# Rig.TUnit.Databases.NoSql.Mongo

Testcontainers-backed MongoDB provider. Ships `MongoFixture`, `MongoFixtureOptions`, `MongoRigBuilder`, the `UseMongo(source, cfg => ...)` fluent entry on `RigBuilder`, plus two helpers: `CollectionPerTestHelper` (per-test collection isolation) and `BsonDiff` (structural document diffing).

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.Mongo
```

## Example

```csharp
public sealed class OrderMongoTests
{
    private readonly MongoFixture _db = new();

    [Before(Test)] public Task Init() => _db.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _db.DisposeAsync();

    [Test]
    public async Task Insert_RoundTrips()
    {
        await using var scope = new CollectionPerTestHelper(_db.Database, IsolationKey.FromName("orders"));
        var orders = scope.GetCollection<BsonDocument>();

        await orders.InsertOneAsync(new BsonDocument { { "sku", "X-1" }, { "qty", 2 } });

        var found = await orders.Find(Builders<BsonDocument>.Filter.Eq("sku", "X-1")).FirstOrDefaultAsync();
        await Assert.That(found["qty"].AsInt32).IsEqualTo(2);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.NoSql`, `Testcontainers.MongoDb`, `MongoDB.Driver`
