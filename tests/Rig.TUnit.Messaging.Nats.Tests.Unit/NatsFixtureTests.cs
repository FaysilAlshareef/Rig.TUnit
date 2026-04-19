using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Nats.Fixtures;
using Rig.TUnit.Messaging.Nats.Options;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

public sealed class NatsFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new NatsFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new NatsFixtureOptions();
        await Assert.That(() => new NatsFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new NatsFixture((NatsFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new NatsFixtureOptions());
        await Assert.That(() => new NatsFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new NatsFixture((IOptions<NatsFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new NatsFixture();

        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TopicName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new NatsFixture();

        var first = fx.TopicName;
        var second = fx.TopicName;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new NatsFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
