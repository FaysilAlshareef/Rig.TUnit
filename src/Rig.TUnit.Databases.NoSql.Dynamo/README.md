# Rig.TUnit.Databases.NoSql.Dynamo

> LocalStack-backed Amazon DynamoDB fixture with `GsiVerifier` for declarative Global-Secondary-Index assertions.

## What this package is

The Rig.TUnit DynamoDB provider. `DynamoFixture` spins a LocalStack
container with the DynamoDB feature enabled and exposes an
`AmazonDynamoDBClient` pointing at it. Ships `GsiVerifier` — a declarative
GSI assertion helper that checks name, partition key, sort key, and
status against the live table schema, saving reams of boilerplate `WAIT
FOR GSI ACTIVE` polling code.

## When to use it

- Integration tests exercising DynamoDB-specific semantics (conditional
  writes, TransactionWrite, GSI projections).
- Asserting table+GSI definitions match the design spec.
- **Not for**: DAX (DynamoDB Accelerator) — LocalStack does not emulate.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (LocalStack image ~400 MB)
- `AWSSDK.DynamoDBv2` (transitive)

## Quick start

```csharp
using Amazon.DynamoDBv2.Model;
using Rig.TUnit.Databases.NoSql.Dynamo.Fixtures;
using Rig.TUnit.Databases.NoSql.Dynamo.Helpers;

await using var fx = new DynamoFixture();
await fx.InitializeAsync();

await fx.Client.CreateTableAsync(new CreateTableRequest
{
    TableName = "orders",
    KeySchema = [new("Pk", Amazon.DynamoDBv2.KeyType.HASH)],
    AttributeDefinitions = [new("Pk", Amazon.DynamoDBv2.ScalarAttributeType.S)],
    BillingMode = Amazon.DynamoDBv2.BillingMode.PAY_PER_REQUEST,
});
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"localstack/localstack:3"` | LocalStack image |
| `StartupTimeoutSeconds` | `int` | `120` | LocalStack boot |
| `AccessKeyId` | `string` | `"test"` | LocalStack default |
| `SecretAccessKey` | `string` | `"test"` | LocalStack default |
| `Region` | `string` | `"us-east-1"` | Region label |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.Dynamo.Fixtures.DynamoFixture`
- `Rig.TUnit.Databases.NoSql.Dynamo.Options.DynamoFixtureOptions`
- `Rig.TUnit.Databases.NoSql.Dynamo.Builder.DynamoRigBuilder`
- `Rig.TUnit.Databases.NoSql.Dynamo.Helpers.GsiVerifier`
- `Rig.TUnit.Databases.NoSql.Dynamo.Helpers.GsiExpectation`

## Per-test isolation

Per-test table naming via `IsolationKey`: `orders_{IsolationKey:short}`.
Table create is ~100 ms (LocalStack's DynamoDB emulation is CPU-fast).
Teardown deletes the table.

## Parallelism + performance

- First-run pull: ~30 s.
- Warm startup: ~10 s.
- Per-test table create + delete: ~150 ms.
- Parallelism: 8+ typical — LocalStack handles concurrent DDL well.

## Troubleshooting

- **`ResourceNotFoundException` after `CreateTableAsync`** — LocalStack
  creates the table asynchronously; `WaitForTableActive` or
  `GsiVerifier.VerifyAsync` polls until `TableStatus=ACTIVE`. Do not assume
  immediate readiness.
- **Eventually consistent reads surprise tests** — LocalStack defaults to
  strong consistency, but DynamoDB proper does not. Set
  `ConsistentRead=true` on your test queries to match the production
  guarantee.

See [docs/troubleshooting.md#dynamodb](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- LocalStack's DynamoDB emulation is close to production but diverges on:
  rate limiting (never throttles), per-table encryption metadata, and
  auto-scaling. Tests asserting these must run against real AWS.
- GSIs can project `ALL`, `KEYS_ONLY`, or `INCLUDE`; `GsiExpectation`
  currently validates name + keys, not projection shape (open issue).

## Benchmarks

See [`DynamoBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/DynamoBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
