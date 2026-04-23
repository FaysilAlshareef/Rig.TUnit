using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Nats.Builder;
using Rig.TUnit.Messaging.Nats.Topology;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration.JetStream;

// T054-RED: compile-fail until T054-GREEN adds WithTopology + INatsTopologyBuilder.
public sealed class NatsTopologyBuilderLiveTests
{
    [Test]
    public async Task WithTopology_CreatesStream_OnJetStream(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"topo-live-{Guid.NewGuid():N}";

        NatsRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseNats(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>                     // CS1061 RED
                    t.Stream(streamName, cfg => cfg           // CS0246 RED
                        .WithSubjects($"topo.{streamName}")));
            }));

        // Act
        await captured!.ApplyTopologyAsync(ct);               // CS1061 RED

        // Assert — stream exists in JetStream
        var stream = await fx.GetStreamAsync(streamName, ct);
        await Assert.That(stream).IsNotNull();
        await Assert.That(stream.Info.Config.Name).IsEqualTo(streamName);
    }

    [Test]
    public async Task WithTopology_CalledTwice_IsIdempotent(CancellationToken ct)
    {
        // Arrange
        var fx         = await SharedNatsJetStreamFixture.GetAsync();
        var streamName = $"topo-idempotent-{Guid.NewGuid():N}";

        NatsRigBuilder? captured = null;
        Action<NatsRigBuilder> configure = builder =>
        {
            captured = builder;
            builder.WithTopology(t =>                         // CS1061 RED
                t.Stream(streamName, cfg =>                   // CS0246 RED
                    cfg.WithSubjects($"idem.{streamName}")));
        };

        new ServiceCollection().AddRigTUnit(rig => rig.UseNats(fx, configure));
        await captured!.ApplyTopologyAsync(ct);               // CS1061 RED

        // Apply again — must not throw
        await Assert.That(async () => await captured.ApplyTopologyAsync(ct))
            .ThrowsNothing();
    }

    [Test]
    public async Task WithTopology_ReturnsSameBuilderForChain(CancellationToken ct)
    {
        var fx = await SharedNatsJetStreamFixture.GetAsync();

        NatsRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseNats(fx, builder =>
            {
                captured = builder;
            }));

        var returned = captured!.WithTopology(_ => { });      // CS1061 RED
        await Assert.That(returned).IsEqualTo(captured);
    }
}
