using Rig.TUnit.Messaging.ServiceBus.Fixtures;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration;

/// <summary>
/// Intentional reuse per A005 audit: container is shared but tests derive per-test
/// names (database / collection / keyspace / key prefix / topic) via IsolationKey or
/// an equivalent primitive, so cross-test isolation is preserved without the cost of
/// a fresh container per test. See planning/post-005-phase-1/SharedFixture-Audit.md.
/// </summary>

/// <summary>
/// Process-wide shared <see cref="ServiceBusFixture"/>. The Microsoft ServiceBus
/// emulator is expensive to boot (~90s with SQL Edge backend), so a single
/// instance is reused across all test classes in this assembly.
/// </summary>
internal static class SharedServiceBusFixture
{
    private static readonly Lazy<Task<ServiceBusFixture>> Instance = new(async () =>
    {
        var fx = new ServiceBusFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<ServiceBusFixture> GetAsync() => Instance.Value;
}
