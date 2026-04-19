using Rig.TUnit.Messaging.Sqs.Fixtures;

namespace Rig.TUnit.Messaging.Sqs.Tests.Integration;

internal static class SharedSqsFixture
{
    private static readonly Lazy<Task<SqsFixture>> Instance = new(async () =>
    {
        var fx = new SqsFixture();
        await fx.InitializeAsync();
        return fx;
    });
    public static Task<SqsFixture> GetAsync() => Instance.Value;
}
