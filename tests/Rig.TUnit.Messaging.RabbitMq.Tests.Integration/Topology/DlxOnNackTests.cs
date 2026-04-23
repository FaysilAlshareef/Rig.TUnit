using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Helpers;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

// T044b-RED: compile-fail until T042-GREEN adds WithTopology + DLX queue config.
public sealed class DlxOnNackTests
{
    [Test]
    public async Task NackedMessage_RoutesViaDlx_ToDlqQueue(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var dlxExchange = $"dlx-{Guid.NewGuid():N}";
        var dlqQueue   = $"dlq-{Guid.NewGuid():N}";
        var mainQueue  = $"main-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>                              // CS1061 RED
                {
                    t.Exchange(dlxExchange, ExchangeType.Direct)       // CS0246 RED
                     .BindQueue(dlqQueue, "dead");
                    t.Queue(mainQueue, cfg =>
                        cfg.WithDeadLetterExchange(dlxExchange, "dead")); // CS1061 RED
                });
            }));

        await captured!.ApplyTopologyAsync(ct);                        // CS1061 RED

        await using var sender   = new RabbitMqEventSender(fx.ConnectionString, mainQueue);
        await using var dlqListener = new RabbitMqListener(fx.ConnectionString, dlqQueue);
        await dlqListener.StartAsync(ct);

        // Act — send a message then nack it so RabbitMQ routes to DLX
        await sender.SendAsync("poison", ct: ct);

        // Assert — DLQ listener eventually receives the dead-lettered message
        await MessageAssert.Within(dlqListener, TimeSpan.FromSeconds(15), expectedCount: 1, ct);
        await Assert.That(dlqListener.Count).IsEqualTo(1);
    }
}
