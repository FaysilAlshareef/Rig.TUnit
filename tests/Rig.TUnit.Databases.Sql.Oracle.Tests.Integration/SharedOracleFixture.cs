using Rig.TUnit.Databases.Sql.Oracle.Fixtures;

namespace Rig.TUnit.Databases.Sql.Oracle.Tests.Integration;

internal static class SharedOracleFixture
{
    private static readonly Lazy<Task<OracleFixture>> Instance = new(async () =>
    {
        var fx = new OracleFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<OracleFixture> GetAsync() => Instance.Value;
}
