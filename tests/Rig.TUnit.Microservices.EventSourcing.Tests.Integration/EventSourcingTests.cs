using Rig.TUnit.Microservices.EventSourcing;

namespace Rig.TUnit.Microservices.EventSourcing.Tests.Integration;

public sealed class EventSourcingTests
{
    // Simple event-sourced aggregate for the harness.
    private sealed record OrderCreated(string OrderId, decimal Total);
    private sealed record OrderApproved(string OrderId);

    private sealed class Order
    {
        private readonly List<object> _pending = new();
        public string? OrderId { get; private set; }
        public decimal Total { get; private set; }
        public bool Approved { get; private set; }
        public IReadOnlyList<object> Pending => _pending;
        public void ClearPending() => _pending.Clear();

        public void Apply(object evt)
        {
            switch (evt)
            {
                case OrderCreated e: OrderId = e.OrderId; Total = e.Total; break;
                case OrderApproved: Approved = true; break;
            }
        }

        public void Create(string id, decimal total)
        {
            var e = new OrderCreated(id, total);
            Apply(e); _pending.Add(e);
        }

        public void Approve()
        {
            if (!Approved)
            {
                var e = new OrderApproved(OrderId!);
                Apply(e); _pending.Add(e);
            }
        }

        public static Order Rehydrate(IEnumerable<object> events)
        {
            var o = new Order();
            foreach (var e in events) o.Apply(e);
            return o;
        }
    }

    private static EventSourcingHarness<Order> NewHarness() => new(
        rehydrate: Order.Rehydrate,
        getRaised: o => o.Pending,
        clearRaised: o => o.ClearPending());

    [Test]
    public async Task When_CreatingOrder_RaisesOrderCreated()
    {
        // Arrange
        var harness = NewHarness();

        // Act
        var result = harness.Given().When(o => o.Create("O-1", 100m));

        // Assert
        result.Then(new OrderCreated("O-1", 100m));
        await Task.CompletedTask;
    }

    [Test]
    public async Task When_ApprovingExistingOrder_RaisesOrderApproved()
    {
        // Arrange
        var harness = NewHarness();

        // Act
        var result = harness.Given(new OrderCreated("O-2", 50m)).When(o => o.Approve());

        // Assert
        result.Then(new OrderApproved("O-2"));
        await Task.CompletedTask;
    }

    [Test]
    public async Task When_ApprovingAlreadyApproved_RaisesNothing()
    {
        // Arrange
        var harness = NewHarness();

        // Act
        var result = harness.Given(new OrderCreated("O-3", 1m), new OrderApproved("O-3"))
                            .When(o => o.Approve());

        // Assert
        result.Then();
        await Task.CompletedTask;
    }

    [Test]
    public async Task AggregateAssert_Raised_WithData_MatchesPredicate()
    {
        // Arrange
        var harness = NewHarness();
        var result = harness.Given().When(o => o.Create("O-4", 200m));

        // Act
        AggregateAssert.Raised<OrderCreated>(result.Raised).WithData(e => e.Total == 200m);

        // Assert
        await Assert.That(result.Raised.Count).IsEqualTo(1);
    }

    [Test]
    public async Task EventCatalogueAssert_HasEvent_VersionMismatch_Throws()
    {
        // Arrange
        var catalogue = new Dictionary<Type, int> { [typeof(OrderCreated)] = 1 };
        var assert = new EventCatalogueAssert(catalogue);

        // Act
        async Task Action() { assert.HasEvent<OrderCreated>(atVersion: 2); await Task.CompletedTask; }

        // Assert
        await Assert.ThrowsAsync<EventSourcingAssertionException>(Action);
    }

    [Test]
    public async Task EventCatalogueAssert_HasHandlerForVersions_PassesForMultiVersion()
    {
        // Arrange
        var catalogue = new Dictionary<Type, int> { [typeof(OrderCreated)] = 1 };
        var assert = new EventCatalogueAssert(catalogue);

        // Act
        assert.HasHandlerForVersions<OrderCreated>(1, 2);

        // Assert — reaching this point without throwing is success.
        await Task.CompletedTask;
    }

    [Test]
    public async Task Then_WithWrongEventCount_ThrowsAssertionException()
    {
        // Arrange
        var harness = NewHarness();
        var result = harness.Given().When(o => o.Create("O-5", 1m));

        // Act
        async Task Action()
        {
            result.Then(new OrderCreated("O-5", 1m), new OrderApproved("O-5"));
            await Task.CompletedTask;
        }

        // Assert
        await Assert.ThrowsAsync<EventSourcingAssertionException>(Action);
    }
}
