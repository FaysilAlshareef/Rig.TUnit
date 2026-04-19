using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.ServiceBus.Fixtures;
using Rig.TUnit.Messaging.ServiceBus.Options;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 backfill tests for <see cref="ServiceBusFixture"/> — exercises every constructor
/// variant, null-guards, AcceptEula requirement, and pre-initialize-state exception paths
/// without starting a container. The container-bound <c>InitializeAsync</c> /
/// <c>DisposeAsync</c> body is covered by the integration suite.
/// </summary>
public sealed class ServiceBusFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new ServiceBusFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new ServiceBusFixtureOptions { ImageTag = "1.1.2" };
        await Assert.That(() => new ServiceBusFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ServiceBusFixture((ServiceBusFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new ServiceBusFixtureOptions());
        await Assert.That(() => new ServiceBusFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ServiceBusFixture((IOptions<ServiceBusFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithAcceptEulaFalse_ThrowsInvalidOperation()
    {
        var options = new ServiceBusFixtureOptions { AcceptEula = false };

        await Assert.That(() => new ServiceBusFixture(options))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new ServiceBusFixture();

        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TopicName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new ServiceBusFixture();

        var first = fx.TopicName;
        var second = fx.TopicName;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new ServiceBusFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
