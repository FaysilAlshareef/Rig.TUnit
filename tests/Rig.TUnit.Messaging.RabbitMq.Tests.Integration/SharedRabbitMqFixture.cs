using Rig.TUnit.Messaging.RabbitMq.Fixtures;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration;

internal static class SharedRabbitMqFixture
{
    private static readonly Lazy<Task<RabbitMqFixture>> Instance = new(async () =>
    {
        var fx = new RabbitMqFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<RabbitMqFixture> GetAsync() => Instance.Value;
}
