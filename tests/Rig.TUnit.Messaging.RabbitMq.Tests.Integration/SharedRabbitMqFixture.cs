using Rig.TUnit.Messaging.RabbitMq.Fixtures;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

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
