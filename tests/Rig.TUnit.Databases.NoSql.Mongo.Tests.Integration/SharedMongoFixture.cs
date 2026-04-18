using Rig.TUnit.Databases.NoSql.Mongo.Fixtures;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration;

internal static class SharedMongoFixture
{
    private static readonly Lazy<Task<MongoFixture>> Instance = new(async () =>
    {
        var fx = new MongoFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<MongoFixture> GetAsync() => Instance.Value;
}
