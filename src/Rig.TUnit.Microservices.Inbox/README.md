# Rig.TUnit.Microservices.Inbox

Inbox pattern — per-aggregate sequence tracking for idempotent event application.
`SequenceTracker` rejects duplicate and out-of-order events; `InboxAssert` offers
a fluent `SequenceApplied(...).Idempotent()` check.

## Install

```xml
<PackageReference Include="Rig.TUnit.Microservices.Inbox" />
```

## Example

```csharp
var tracker = new SequenceTracker();
tracker.TryApply("agg-1", 5);          // true
tracker.TryApply("agg-1", 5);          // false — idempotent re-apply
tracker.TryApply("agg-1", 4);          // false — out of order

InboxAssert.SequenceApplied(tracker, "agg-1", 5).Idempotent();
```

Spec: `003-rig-tunit-ecosystem-expansion` — US8.
