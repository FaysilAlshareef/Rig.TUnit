using Rig.TUnit.Databases.Sql.Postgresql.Fixtures;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Integration;

internal static class SharedPostgresFixture
{
    private static readonly Lazy<Task<PostgresFixture>> Instance = new(async () =>
    {
        var fx = new PostgresFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<PostgresFixture> GetAsync() => Instance.Value;
}
