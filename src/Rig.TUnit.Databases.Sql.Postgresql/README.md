# Rig.TUnit.Databases.Sql.Postgresql

Testcontainers-backed PostgreSQL provider. Ships `PostgresFixture`, `PostgresFixtureOptions`, `PostgresRigBuilder`, the `UsePostgres(source, sql => ...)` fluent entry on `RigBuilder`, and an EF Core wrapper `UsePostgres(connectionString)` on `DbContextOptionsBuilder`.

## Install

```
dotnet add package Rig.TUnit.Databases.Sql.Postgresql
```

## Example

```csharp
public sealed class OrderTests
{
    private readonly PostgresFixture _db = new();

    [Before(Test)] public Task Init() => _db.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _db.DisposeAsync();

    [Test]
    public async Task CreateOrder_PersistsRow()
    {
        await using var helper = new DbContextHelper<OrderDb>(new OrderDb(
            new DbContextOptionsBuilder<OrderDb>().UsePostgres(_db.ConnectionString).Options));

        await helper.InsertAsync(new Order(id: 1, customerId: 42));

        var rows = await helper.QueryAsync(ctx => ctx.Orders);
        Assert.That(rows.Count).IsEqualTo(1);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.Sql`, `Testcontainers.PostgreSql`, `Npgsql.EntityFrameworkCore.PostgreSQL`
