using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.RabbitMq.Fixtures;
using Rig.TUnit.Messaging.RabbitMq.Options;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

public sealed class RabbitMqFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new RabbitMqFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new RabbitMqFixtureOptions();
        await Assert.That(() => new RabbitMqFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new RabbitMqFixture((RabbitMqFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new RabbitMqFixtureOptions());
        await Assert.That(() => new RabbitMqFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new RabbitMqFixture((IOptions<RabbitMqFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new RabbitMqFixture();

        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TopicName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new RabbitMqFixture();

        var first = fx.TopicName;
        var second = fx.TopicName;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new RabbitMqFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
