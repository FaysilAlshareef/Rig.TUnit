using Rig.TUnit.Messaging.Kafka.Fixtures;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

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
