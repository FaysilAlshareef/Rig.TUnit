using Rig.TUnit.Databases.Sql.MySql.Fixtures;

namespace Rig.TUnit.Databases.Sql.MySql.Tests.Integration;

internal static class SharedMySqlFixture
{
    private static readonly Lazy<Task<MySqlFixture>> Instance = new(async () =>
    {
        var fx = new MySqlFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<MySqlFixture> GetAsync() => Instance.Value;
}
