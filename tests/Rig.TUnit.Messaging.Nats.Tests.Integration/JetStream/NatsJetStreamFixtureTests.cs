using Rig.TUnit.Messaging.Nats.Fixtures;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T051-RED: compile-fail until T051-GREEN adds NatsJetStreamFixture.
public sealed class NatsJetStreamFixtureTests
{
    [Test]
    public async Task NatsJetStreamFixture_Initialize_OpenesConnectionAndJetStreamContext(CancellationToken ct)
    {
        // Arrange & Act
        var fx = await SharedNatsJetStreamFixture.GetAsync();

        // Assert — JetStream context is available and connection is functional
        await Assert.That(fx).IsNotNull();
        await Assert.That(fx.JetStream).IsNotNull();  // CS1061 RED until T051-GREEN
        await Assert.That(fx.ConnectionString).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task NatsJetStreamFixture_EnsureStream_CreatesAccessibleStream(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"fixture-test-{Guid.NewGuid():N}";
        var subject    = $"test.{streamName}";

        // Act
        await fx.EnsureStreamAsync(streamName, [subject], ct: ct);  // CS1061 RED

        // Assert — stream exists in JetStream
        var stream = await fx.GetStreamAsync(streamName, ct);  // CS1061 RED
        await Assert.That(stream).IsNotNull();
        await Assert.That(stream.Info.Config.Name).IsEqualTo(streamName);
    }

    [Test]
    public async Task NatsJetStreamFixture_EnsureStream_IsIdempotent(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"idempotent-{Guid.NewGuid():N}";
        var subject    = $"idempotent.{streamName}";

        // Act — apply twice, must not throw
        await fx.EnsureStreamAsync(streamName, [subject], ct: ct);  // CS1061 RED
        await fx.EnsureStreamAsync(streamName, [subject], ct: ct);  // CS1061 RED — idempotent

        // Assert — stream still valid after second call
        var stream = await fx.GetStreamAsync(streamName, ct);  // CS1061 RED
        await Assert.That(stream.Info.Config.Name).IsEqualTo(streamName);
    }
}
