using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Assertions;
using Rig.TUnit.Messaging.RabbitMq.Builder;
using Rig.TUnit.Messaging.RabbitMq.Helpers;
using Rig.TUnit.Messaging.RabbitMq.Topology;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration.Topology;

/// <summary>
/// Verifies that <c>IRabbitMqQueueConfig.WithDeadLetterExchange</c> wires up
/// <c>x-dead-letter-exchange</c> + <c>x-dead-letter-routing-key</c> correctly. The test
/// uses TTL-based dead-lettering (messages expire immediately on the main queue and
/// AMQP routes them to the DLX) instead of consumer NACK-based dead-lettering, because
/// the rig sender doesn't currently expose a NACK API and the listener runs autoAck.
/// Both expiry and NACK exercise the same DLX routing path inside RabbitMQ.
/// </summary>
public sealed class DlxOnNackTests
{
    [Test]
    public async Task ExpiredMessage_RoutesViaDlx_ToDlqQueue(CancellationToken ct)
    {
        // Arrange
        var fx = await SharedRabbitMqFixture.GetAsync();
        var dlxExchange = $"dlx-{Guid.NewGuid():N}";
        var dlqQueue = $"dlq-{Guid.NewGuid():N}";
        var mainQueue = $"main-{Guid.NewGuid():N}";

        RabbitMqRigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig =>
            rig.UseRabbitMq(fx, builder =>
            {
                captured = builder;
                builder.WithTopology(t =>
                {
                    t.Exchange(dlxExchange, ExchangeType.Direct)
                     .BindQueue(dlqQueue, "dead");
                    t.Queue(mainQueue, cfg => cfg
                        .WithMessageTtl(TimeSpan.FromMilliseconds(1))
                        .WithDeadLetterExchange(dlxExchange, "dead"));
                });
            }));

        await captured!.ApplyTopologyAsync(ct);

        await using var sender = new RabbitMqEventSender(fx.ConnectionString, mainQueue);
        await using var dlqListener = new RabbitMqListener(fx.ConnectionString, dlqQueue);
        await dlqListener.StartAsync(ct);

        // Act — message expires immediately (TTL=1ms) and the broker routes to DLX
        await sender.SendAsync("poison", ct: ct);

        // Assert — DLQ listener eventually receives the dead-lettered message
        await MessageAssert.Within(dlqListener, TimeSpan.FromSeconds(30), expectedCount: 1, ct);
        await Assert.That(dlqListener.Count).IsEqualTo(1);
    }
}
