using Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Integration;

internal static class SharedKurrentDbFixture
{
    private static readonly Lazy<Task<KurrentDbFixture>> Instance = new(async () =>
    {
        var fx = new KurrentDbFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<KurrentDbFixture> GetAsync() => Instance.Value;
}
