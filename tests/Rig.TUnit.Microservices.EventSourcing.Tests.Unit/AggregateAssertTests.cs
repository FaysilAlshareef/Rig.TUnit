using Rig.TUnit.Microservices.EventSourcing.Assertions;

namespace Rig.TUnit.Microservices.EventSourcing.Tests.Unit;

public sealed class AggregateAssertTests
{
    private sealed record OrderCreated(string OrderId, decimal Amount);
    private sealed record OrderShipped(string OrderId);
    private sealed record TestAggregate(string Id);

    [Test]
    public async Task For_NullRaised_Throws()
    {
        await Assert.That(() => AggregateFluentAssert.For(new TestAggregate("x"), null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Raised_MatchingEvent_CountPositive()
    {
        var raised = new object[] { new OrderCreated("o1", 10m) };
        var assertion = AggregateFluentAssert.For(new TestAggregate("x"), raised).Raised<OrderCreated>();
        await Assert.That(assertion.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Exactly_Matches_Passes()
    {
        var raised = new object[] { new OrderCreated("o1", 10m), new OrderCreated("o2", 20m) };
        AggregateFluentAssert.For(new TestAggregate("x"), raised).Raised<OrderCreated>().Exactly(2);
        await Task.CompletedTask;
    }

    [Test]
    public async Task Exactly_Mismatch_Throws()
    {
        var raised = new object[] { new OrderCreated("o1", 10m) };
        await Assert.That(() => AggregateFluentAssert.For(new TestAggregate("x"), raised).Raised<OrderCreated>().Exactly(2))
            .ThrowsExactly<AggregateAssertionException>();
    }

    [Test]
    public async Task AtLeast_Enough_Passes()
    {
        var raised = new object[] { new OrderCreated("o1", 10m), new OrderCreated("o2", 20m) };
        AggregateFluentAssert.For(new TestAggregate("x"), raised).Raised<OrderCreated>().AtLeast(1);
        await Task.CompletedTask;
    }

    [Test]
    public async Task AtLeast_Short_Throws()
    {
        var raised = new object[] { new OrderCreated("o1", 10m) };
        await Assert.That(() => AggregateFluentAssert.For(new TestAggregate("x"), raised).Raised<OrderCreated>().AtLeast(5))
            .ThrowsExactly<AggregateAssertionException>();
    }

    [Test]
    public async Task WithData_PredicateMatches_Passes()
    {
        var raised = new object[] { new OrderCreated("o1", 10m), new OrderCreated("o2", 20m) };
        AggregateFluentAssert.For(new TestAggregate("x"), raised)
            .Raised<OrderCreated>()
            .WithData(e => e.Amount == 20m);
        await Task.CompletedTask;
    }

    [Test]
    public async Task WithData_PredicateMisses_Throws()
    {
        var raised = new object[] { new OrderCreated("o1", 10m) };
        await Assert.That(() => AggregateFluentAssert.For(new TestAggregate("x"), raised)
                .Raised<OrderCreated>()
                .WithData(e => e.Amount == 99m))
            .ThrowsExactly<AggregateAssertionException>();
    }

    [Test]
    public async Task NotRaised_NoMatch_Passes()
    {
        var raised = new object[] { new OrderCreated("o1", 10m) };
        AggregateFluentAssert.For(new TestAggregate("x"), raised).NotRaised<OrderShipped>();
        await Task.CompletedTask;
    }

    [Test]
    public async Task NotRaised_WithMatch_Throws()
    {
        var raised = new object[] { new OrderShipped("o1") };
        await Assert.That(() => AggregateFluentAssert.For(new TestAggregate("x"), raised).NotRaised<OrderShipped>())
            .ThrowsExactly<AggregateAssertionException>();
    }
}
