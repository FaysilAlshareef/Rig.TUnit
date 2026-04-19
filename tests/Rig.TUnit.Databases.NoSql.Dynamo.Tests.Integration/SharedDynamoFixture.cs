using Rig.TUnit.Databases.NoSql.Dynamo.Fixtures;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration;

internal static class SharedDynamoFixture
{
    private static readonly Lazy<Task<DynamoFixture>> Instance = new(async () =>
    {
        var fx = new DynamoFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<DynamoFixture> GetAsync() => Instance.Value;
}
