using Rig.TUnit.Databases.NoSql.EventStore.Fixtures;

namespace Rig.TUnit.Databases.NoSql.EventStore.Tests.Integration;

internal static class SharedEventStoreFixture
{
    private static readonly Lazy<Task<EventStoreFixture>> Instance = new(async () =>
    {
        var fx = new EventStoreFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<EventStoreFixture> GetAsync() => Instance.Value;
}
