using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Sqs.Builder;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration.Topology;

// T031-RED: compile-fail until T031-GREEN adds SqsRigBuilder.WithTopology/ApplyTopologyAsync.
public sealed class SqsTopologyBuilderLiveTests
{
    [Test]
    public async Task WithTopology_CreatesQueue_OnLocalStack(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedSqsFixture.GetAsync();
        var queueName = $"topo-live-{Guid.NewGuid():N}";

        SqsRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseSqs(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>
                    t.Queue(queueName));
            }));

        // Act
        await captured!.ApplyTopologyAsync(ct);

        // Assert — queue was created (GetQueueUrl does not throw)
        var url = await fx.Client.GetQueueUrlAsync(queueName, ct);
        await Assert.That(url.QueueUrl).IsNotNull();
    }

    [Test]
    public async Task WithTopology_CalledTwice_IsIdempotent(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedSqsFixture.GetAsync();
        var queueName = $"topo-idem-{Guid.NewGuid():N}";

        SqsRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseSqs(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t => t.Queue(queueName));
            }));

        // Act — apply twice; second call must not throw (idempotent create-if-exists)
        await captured!.ApplyTopologyAsync(ct);
        await Assert.That(async () =>
            await captured!.ApplyTopologyAsync(ct))
            .ThrowsNothing();
    }

    [Test]
    public async Task WithTopology_ReturnsSameBuilderForChain(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedSqsFixture.GetAsync();

        SqsRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseSqs(fx, builder =>
            {
                captured = builder;
                var returned = builder.WithTopology(t =>
                    t.Queue($"chain-{Guid.NewGuid():N}"));
                Assert.That(returned).IsEqualTo(builder);
            }));

        await Assert.That(captured).IsNotNull();
    }
}
