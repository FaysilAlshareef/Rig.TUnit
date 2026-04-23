using Rig.TUnit.Messaging.Nats.Fixtures;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration;

// Shared singleton JetStream fixture — container is reused; tests isolate via unique stream names.
internal static class SharedNatsJetStreamFixture
{
    private static readonly Lazy<Task<NatsJetStreamFixture>> Instance = new(async () =>
    {
        var fx = new NatsJetStreamFixture();     // CS0246 RED until T051-GREEN
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<NatsJetStreamFixture> GetAsync() => Instance.Value;
}
