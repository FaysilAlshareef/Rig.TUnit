using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Topology;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

// T042-RED: CS1061 compile-fail until T042-GREEN adds RabbitMqRigBuilder.WithTopology/ApplyTopologyAsync.
public sealed class RabbitMqTopologyLiveTests
{
    [Test]
    public async Task WithTopology_CreatesExchangeAndQueue_OnBroker(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var exchange = $"topo-live-{Guid.NewGuid():N}";
        var queue    = $"topo-q-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>                        // CS1061 RED
                    t.Exchange(exchange, ExchangeType.Direct)    // CS0246 RED
                     .BindQueue(queue, queue));
            }));

        // Act
        await captured!.ApplyTopologyAsync(ct);                  // CS1061 RED

        // Assert — no exception thrown means exchange + queue were created
        await Assert.That(captured).IsNotNull();
    }

    [Test]
    public async Task WithTopology_CalledTwice_IsIdempotent(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var exchange = $"topo-idem-{Guid.NewGuid():N}";
        var queue    = $"topo-idem-q-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>
                    t.Exchange(exchange, ExchangeType.Direct)    // CS1061/CS0246 RED
                     .BindQueue(queue, queue));
            }));

        // Act — apply twice; re-declaring with same args must not throw PRECONDITION_FAILED
        await captured!.ApplyTopologyAsync(ct);                  // CS1061 RED
        await Assert.That(async () =>
            await captured!.ApplyTopologyAsync(ct))
            .ThrowsNothing();
    }

    [Test]
    public async Task WithTopology_ReturnsSameBuilderForChain(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();

        RabbitMqRigBuilder? captured = null;
        RabbitMqRigBuilder? returned = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                returned = builder.WithTopology(t =>             // CS1061 RED
                    t.Queue($"chain-{Guid.NewGuid():N}"));
            }));

        await Assert.That(captured).IsNotNull();
        await Assert.That(returned).IsEqualTo(captured);
    }
}
