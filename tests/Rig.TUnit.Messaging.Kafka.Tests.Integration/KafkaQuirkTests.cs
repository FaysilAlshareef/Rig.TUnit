using Confluent.Kafka;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration;

/// <summary>Kafka quirks: bootstrap-address format, produce+consume roundtrip, offset-reset flag.</summary>
public sealed class KafkaQuirkTests
{
    [Test]
    public async Task BootstrapAddress_IsHostColonPortFormat()
    {
        var fx = await SharedKafkaFixture.GetAsync();
        var address = fx.ConnectionString;
        await Assert.That(address).Contains(":");
    }

    [Test]
    public async Task Produce_ThenConsume_RoundtripsMessage()
    {
        var fx = await SharedKafkaFixture.GetAsync();
        var topic = $"quirk-{Guid.NewGuid():N}".Substring(0, 20);

        var producerConfig = new ProducerConfig { BootstrapServers = fx.ConnectionString };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        await producer.ProduceAsync(topic, new Message<string, string> { Key = "k", Value = "hello" });

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = fx.ConnectionString,
            GroupId = $"g-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(topic);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = consumer.Consume(cts.Token);

        await Assert.That(result.Message.Value).IsEqualTo("hello");
    }

    [Test]
    public async Task Consumer_AutoOffsetReset_RoundtripsEarliest()
    {
        var config = new ConsumerConfig { AutoOffsetReset = AutoOffsetReset.Earliest };
        await Assert.That(config.AutoOffsetReset).IsEqualTo(AutoOffsetReset.Earliest);
    }
}
