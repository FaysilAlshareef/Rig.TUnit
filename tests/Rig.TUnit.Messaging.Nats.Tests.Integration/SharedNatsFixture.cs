using Rig.TUnit.Messaging.Nats.Fixtures;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration;

internal static class SharedNatsFixture
{
    private static readonly Lazy<Task<NatsFixture>> Instance = new(async () =>
    {
        var fx = new NatsFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<NatsFixture> GetAsync() => Instance.Value;
}
