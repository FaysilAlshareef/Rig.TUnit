# Rig.TUnit.Microservices.EventSourcing

Given/When/Then harness for event-sourced aggregates. Ships
`EventSourcingHarness<TAggregate>`, `AggregateAssert.Raised<T>().WithData(...)`,
and `EventCatalogueAssert` for schema-evolution verification.

## Install

```xml
<PackageReference Include="Rig.TUnit.Microservices.EventSourcing" />
```

## Example

```csharp
var harness = new EventSourcingHarness<Order>(
    rehydrate:   events => Order.Rehydrate(events),
    getRaised:   o => o.Pending,
    clearRaised: o => o.ClearPending());

harness.Given(new OrderCreated("O-1", 100m))
       .When(o => o.Approve())
       .Then(new OrderApproved("O-1"));
```

Spec: `003-rig-tunit-ecosystem-expansion` — FR:102, US8.
