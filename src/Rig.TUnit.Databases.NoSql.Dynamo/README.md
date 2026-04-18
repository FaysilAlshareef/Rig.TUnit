# Rig.TUnit.Databases.NoSql.Dynamo

LocalStack-backed Amazon DynamoDB provider. Ships `DynamoFixture`, `DynamoFixtureOptions`, `DynamoRigBuilder`, the `UseDynamo(source, cfg => ...)` fluent entry on `RigBuilder`, and `GsiVerifier` — a declarative Global-Secondary-Index assertion helper that checks name, partition key, sort key, and status against a table's current schema.

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.Dynamo
```

## Example

```csharp
public sealed class OrderDynamoTests
{
    private readonly DynamoFixture _db = new();

    [Before(Test)] public Task Init() => _db.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _db.DisposeAsync();

    [Test]
    public async Task CreateTable_HasExpectedGsis()
    {
        await _db.Client.CreateTableAsync(new CreateTableRequest
        {
            TableName = "orders",
            KeySchema = [new("Pk", KeyType.HASH)],
            AttributeDefinitions =
            [
                new("Pk", ScalarAttributeType.S),
                new("Email", ScalarAttributeType.S),
            ],
            BillingMode = BillingMode.PAY_PER_REQUEST,
            GlobalSecondaryIndexes =
            [
                new()
                {
                    IndexName = "by-email",
                    KeySchema = [new("Email", KeyType.HASH)],
                    Projection = new() { ProjectionType = ProjectionType.ALL },
                },
            ],
        });

        await GsiVerifier.VerifyAsync(_db.Client, "orders",
            [new GsiExpectation("by-email", "Email")]);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.NoSql`, `Testcontainers.LocalStack`, `AWSSDK.DynamoDBv2`
