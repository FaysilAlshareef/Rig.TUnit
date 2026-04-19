using Rig.TUnit.Messaging.Kafka.Fixtures;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration;

internal static class SharedKafkaFixture
{
    private static readonly Lazy<Task<KafkaFixture>> Instance = new(async () =>
    {
        var fx = new KafkaFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<KafkaFixture> GetAsync() => Instance.Value;
}
