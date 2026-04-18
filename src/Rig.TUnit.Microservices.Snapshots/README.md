# Rig.TUnit.Microservices.Snapshots

Snapshot-testing assertion compatible with Verify.TUnit file naming
(`{name}.received.*` / `{name}.verified.*`). Microservice-opinionated scrubbers
replace correlation/causation IDs, event IDs, timestamps, sequence numbers,
connection strings, and filesystem paths with deterministic placeholders.

## Install

```xml
<PackageReference Include="Rig.TUnit.Microservices.Snapshots" />
```

## Example

```csharp
var result = await SnapshotAssert.MatchJson(payload, name: "order-created", directory: "__snapshots__");
// First run creates {name}.verified.json — review & commit.
// Subsequent runs compare; mismatch throws SnapshotAssertionException with line diff.
```

## Scrubbers applied (in order)

| Pattern                                   | Replacement         |
|-------------------------------------------|---------------------|
| GUID                                      | `{Guid}`            |
| ISO-8601 timestamp                        | `{Timestamp}`       |
| `"CorrelationId" / "CausationId"` values  | `{CorrelationLike}` |
| `"EventId" / "MessageId"` values          | `{EventLike}`       |
| `"Sequence"` numeric values               | `{Sequence}`        |
| SQL Server connection strings             | `{ConnectionString}`|
| Windows / Unix absolute paths             | `{WindowsPath}` / `{UnixPath}` |

Spec: `003-rig-tunit-ecosystem-expansion` — FR:101, US8, C-003.
