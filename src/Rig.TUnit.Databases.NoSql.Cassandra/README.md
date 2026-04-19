# Rig.TUnit.Databases.NoSql.Cassandra

Testcontainers-backed Apache Cassandra provider. Ships `CassandraFixture`, `CassandraFixtureOptions`, `CassandraRigBuilder`, the `UseCassandra(source, cfg => ...)` fluent entry on `RigBuilder`, plus the `KeyspacePerTestHelper` — pure `BuildSafeKeyspace` validator (CQL identifier whitelist, 48-char cap, injection-safe) and an async factory that issues `CREATE KEYSPACE` / `DROP KEYSPACE` per test.

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.Cassandra
```

## Example

```csharp
public sealed class OrderCassandraTests
{
    private readonly CassandraFixture _db = new();

    [Before(Test)] public Task Init() => _db.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _db.DisposeAsync();

    [Test]
    public async Task Insert_RoundTrips()
    {
        var key = IsolationKey.FromName("orders");
        await using var scope = await KeyspacePerTestHelper.CreateAsync(_db.Session, key, prefix: "orders");

        await _db.Session.ExecuteAsync(new SimpleStatement(
            $"CREATE TABLE {scope.KeyspaceName}.items (sku text PRIMARY KEY, qty int)"));
        await _db.Session.ExecuteAsync(new SimpleStatement(
            $"INSERT INTO {scope.KeyspaceName}.items (sku, qty) VALUES ('X-1', 2)"));

        var rs = await _db.Session.ExecuteAsync(new SimpleStatement(
            $"SELECT qty FROM {scope.KeyspaceName}.items WHERE sku = 'X-1'"));
        await Assert.That(rs.Single().GetValue<int>("qty")).IsEqualTo(2);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.NoSql`, `Testcontainers.Cassandra`, `CassandraCSharpDriver`
