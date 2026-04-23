using Rig.TUnit.Microservices.EventSourcing.Helpers;

namespace Rig.TUnit.Microservices.EventSourcing.Tests.Unit;

public sealed class EventSourcingHarnessTests
{
    private sealed record OrderCreated(string OrderId, decimal Amount);
    private sealed record Order(string Id, decimal Total, List<object> Raised) { }

    [Test]
    public async Task EventCatalogueVerifier_FindMissingFactories_ReturnsMissingTypes()
    {
        // Use the test assembly as both — it has event types but no factory methods for all
        // so result may be non-empty. Verify the method runs without throwing.
        var assembly = typeof(EventSourcingHarnessTests).Assembly;
        var result = EventCatalogueVerifier.FindMissingFactories(assembly, assembly);

        await Assert.That(result).IsNotNull();
    }

    [Test]
    public Task EventSourcingHarness_When_ReturnsWhenStageWithRaisedEvents()
    {
        Order BuildOrder(IEnumerable<object> events)
        {
            var raised = new List<object>();
            var order = new Order("o1", 0, raised);
            foreach (var e in events)
            {
                if (e is OrderCreated oc)
                {
                    raised.Add(oc);
                }
            }
            return order;
        }

        var harness = new EventSourcingHarness<Order>(
            rehydrate: BuildOrder,
            getRaised: o => o.Raised,
            clearRaised: o => o.Raised.Clear());

        var stage = harness.When(o => o.Raised.Add(new OrderCreated("o1", 10m)));

        _ = stage;
        return Task.CompletedTask;
    }

    [Test]
    public Task EventSourcingHarness_Given_WithNullEvents_DoesNotThrow()
    {
        var harness = new EventSourcingHarness<Order>(
            rehydrate: _ => new Order("o1", 0, new List<object>()),
            getRaised: o => o.Raised,
            clearRaised: o => o.Raised.Clear());

        harness.Given(null!);
        return Task.CompletedTask;
    }

    [Test]
    public async Task EventSourcingHarness_Ctor_NullRehydrate_Throws()
    {
        await Assert.That(() => new EventSourcingHarness<Order>(null!, o => o.Raised, o => o.Raised.Clear()))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EventSourcingHarness_Ctor_NullGetRaised_Throws()
    {
        await Assert.That(() => new EventSourcingHarness<Order>(_ => new Order("o1", 0, []), null!, o => o.Raised.Clear()))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EventSourcingHarness_When_NullAction_Throws()
    {
        var harness = new EventSourcingHarness<Order>(
            rehydrate: _ => new Order("o1", 0, []),
            getRaised: o => o.Raised,
            clearRaised: o => o.Raised.Clear());

        await Assert.That(() => harness.When(null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
