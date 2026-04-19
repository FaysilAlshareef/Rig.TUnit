using Rig.TUnit.Messaging.ServiceBus.Fixtures;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration;

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
