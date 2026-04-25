using Rig.TUnit.Messaging.Nats.Fixtures;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

/// <summary>
/// Guard-only tests for <see cref="NatsJetStreamFixture"/>. The container is
/// not started — these only exercise the pre-initialize property paths and
/// the constructor null/whitespace-guards.
/// </summary>
public sealed class NatsJetStreamFixtureTests
{
    [Test]
    public async Task Ctor_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() => new NatsJetStreamFixture(null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new NatsJetStreamFixture())
            .ThrowsNothing();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperationException()
    {
        var fx = new NatsJetStreamFixture();

        await Assert.That(() => _ = fx.ConnectionString)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task JetStream_BeforeInitialize_ThrowsInvalidOperationException()
    {
        var fx = new NatsJetStreamFixture();

        await Assert.That(() => _ = fx.JetStream)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new NatsJetStreamFixture();

        await Assert.That(async () => await fx.DisposeAsync())
            .ThrowsNothing();
    }
}
