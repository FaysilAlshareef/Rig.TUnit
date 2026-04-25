using Rig.TUnit.Messaging.Nats.Fixtures;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (stream / consumer / subject) via IsolationKey or unique GUIDs, so cross-test
/// isolation is preserved without the cost of a fresh JetStream container per test.
/// See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>
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
