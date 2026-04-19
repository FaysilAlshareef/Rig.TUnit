using Rig.TUnit.Databases.NoSql.Cassandra.Fixtures;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Integration;

internal static class SharedCassandraFixture
{
    private static readonly Lazy<Task<CassandraFixture>> Instance = new(async () =>
    {
        var fx = new CassandraFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<CassandraFixture> GetAsync() => Instance.Value;
}
