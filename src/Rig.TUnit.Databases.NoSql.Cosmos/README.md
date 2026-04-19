# Rig.TUnit.Databases.NoSql.Cosmos

Testcontainers-backed Cosmos emulator fixture for Rig.TUnit. Uses the
`mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` Linux
emulator image via the generic Testcontainers API (the dedicated
`Testcontainers.CosmosDb` module targets the legacy Windows emulator, which is
incompatible — see testcontainers-dotnet#1306).

Windows runners cannot host the Linux emulator; CI gates Cosmos tests with
`[Category("cosmos")]` + runtime `OperatingSystem.IsWindows()` skip.

## Install

```bash
dotnet add package Rig.TUnit.Databases.NoSql.Cosmos
```

## Quick start

```csharp
using Microsoft.Azure.Cosmos;
using Rig.TUnit.Databases.NoSql.Cosmos.Fixtures;
using Rig.TUnit.Databases.NoSql.Cosmos.Helpers;

await using var fx = new CosmosFixture();
await fx.InitializeAsync();

using var client = new CosmosClient(fx.ConnectionString);
var db = await client.CreateDatabaseIfNotExistsAsync(fx.DatabaseName);

var rus = new RuChargeCapture();
var response = await db.Database.CreateContainerIfNotExistsAsync("orders", "/tenantId");
rus.Record("container-create", response.RequestCharge);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseCosmos(RigConnect.FromValue(fx.ConnectionString), cfg => { })
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `Image` | `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview` | Emulator container image |
| `StartupTimeoutSeconds` | `300` | Cosmos emulator boot is slow |
| `DatabaseName` | `rigtunit` | Default database on startup |
| `HttpsGatewayPort` | `8081` | Emulator gateway port |

## Helpers

- `RuChargeCapture` — thread-safe RU charge recorder; assert per-operation and
  total RU budgets in tests.
- `PartitionKeyDistributionChecker` — pure helper computing max-share +
  normalised Shannon entropy + threshold predicate for hot-partition detection.
