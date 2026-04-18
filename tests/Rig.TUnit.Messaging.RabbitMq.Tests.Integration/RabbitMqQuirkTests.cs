using RabbitMQ.Client;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration;

/// <summary>RabbitMQ quirks: AMQP scheme, direct routing, DLX dead-letter capture.</summary>
public sealed class RabbitMqQuirkTests
{
    [Test]
    public async Task ConnectionString_WhenFixtureInitialised_StartsWithAmqpScheme()
    {
        var fx = await SharedRabbitMqFixture.GetAsync();
        await Assert.That(fx.ConnectionString).StartsWith("amqp://");
    }

    [Test]
    public async Task Publish_OnDirectExchange_RoutesByBindingKey()
    {
        var fx = await SharedRabbitMqFixture.GetAsync();
        var factory = new ConnectionFactory { Uri = new Uri(fx.ConnectionString) };
        using var conn = await factory.CreateConnectionAsync();
        using var ch = await conn.CreateChannelAsync();

        var ex = $"ex-{Guid.NewGuid():N}";
        var q = $"q-{Guid.NewGuid():N}";
        await ch.ExchangeDeclareAsync(ex, ExchangeType.Direct, durable: false, autoDelete: true);
        await ch.QueueDeclareAsync(q, durable: false, exclusive: true, autoDelete: true, arguments: null);
        await ch.QueueBindAsync(q, ex, routingKey: "alpha");

        await ch.BasicPublishAsync(ex, "alpha", System.Text.Encoding.UTF8.GetBytes("hi"));
        var msg = await ch.BasicGetAsync(q, autoAck: true);

        await Assert.That(msg).IsNotNull();
        await Assert.That(System.Text.Encoding.UTF8.GetString(msg!.Body.Span)).IsEqualTo("hi");
    }

    [Test]
    public async Task Reject_WhenDlxConfigured_RoutesToDeadLetterQueue()
    {
        var fx = await SharedRabbitMqFixture.GetAsync();
        var factory = new ConnectionFactory { Uri = new Uri(fx.ConnectionString) };
        using var conn = await factory.CreateConnectionAsync();
        using var ch = await conn.CreateChannelAsync();

        var dlx = $"dlx-{Guid.NewGuid():N}";
        var dlq = $"dlq-{Guid.NewGuid():N}";
        var mainQ = $"main-{Guid.NewGuid():N}";

        await ch.ExchangeDeclareAsync(dlx, ExchangeType.Fanout, durable: false, autoDelete: true);
        await ch.QueueDeclareAsync(dlq, durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(dlq, dlx, "");
        await ch.QueueDeclareAsync(mainQ, durable: false, exclusive: true, autoDelete: true,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = dlx });

        await ch.BasicPublishAsync("", mainQ, System.Text.Encoding.UTF8.GetBytes("rejected"));
        var got = await ch.BasicGetAsync(mainQ, autoAck: false);
        await ch.BasicRejectAsync(got!.DeliveryTag, requeue: false);

        BasicGetResult? dead = null;
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTimeOffset.UtcNow < deadline)
        {
            dead = await ch.BasicGetAsync(dlq, autoAck: true);
            if (dead is not null) break;
            await Task.Delay(100);
        }

        await Assert.That(dead).IsNotNull();
    }
}
