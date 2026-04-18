using Rig.TUnit.Databases.NoSql.ElasticSearch.Fixtures;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration;

internal static class SharedElasticSearchFixture
{
    private static readonly Lazy<Task<ElasticSearchFixture>> Instance = new(async () =>
    {
        var fx = new ElasticSearchFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<ElasticSearchFixture> GetAsync() => Instance.Value;
}
