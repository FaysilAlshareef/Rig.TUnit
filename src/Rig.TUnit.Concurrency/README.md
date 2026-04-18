# Rig.TUnit.Concurrency

Concurrency + idempotency testing helpers. Ships `ConcurrencyAssert.TwoWriters`
for two-writers-one-wins assertions against any concurrency-exception type
(EF `DbUpdateConcurrencyException`, Mongo `MongoWriteException`, Cosmos 412, ...),
`Precondition.IfMatchFails` / `NotModified` for HTTP ETag/If-Match flows, and
`SequenceIdempotencyChecker` for replay safety.

## Install

```xml
<PackageReference Include="Rig.TUnit.Concurrency" />
```

## Example

```csharp
await ConcurrencyAssert.TwoWriters(order).OneWinsWith<DbUpdateConcurrencyException>(
    a => a.TryUpdateAsync(newValue: 1),
    b => b.TryUpdateAsync(newValue: 2));
```

Spec: `003-rig-tunit-ecosystem-expansion` — US9.
