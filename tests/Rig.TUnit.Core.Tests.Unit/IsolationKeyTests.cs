namespace Rig.TUnit.Core.Tests.Unit;

public sealed class IsolationKeyTests
{
    [Test]
    public async Task FromName_WithShortName_ReturnsNameAndHash()
    {
        var key = IsolationKey.FromName("SqlServerFixtureTests.InitializeAsync");

        await Assert.That(key.Value).Contains("_");
        await Assert.That(key.Value.Length).IsLessThanOrEqualTo(29);
    }

    [Test]
    public async Task FromName_SameInput_ReturnsSameKey_Deterministic()
    {
        var key1 = IsolationKey.FromName("MyTestClass.MyTestMethod");
        var key2 = IsolationKey.FromName("MyTestClass.MyTestMethod");

        await Assert.That(key1.Value).IsEqualTo(key2.Value);
    }

    [Test]
    public async Task FromName_DifferentInput_ReturnsDifferentKey()
    {
        var key1 = IsolationKey.FromName("MyTestClass.MethodA");
        var key2 = IsolationKey.FromName("MyTestClass.MethodB");

        await Assert.That(key1.Value).IsNotEqualTo(key2.Value);
    }

    [Test]
    public async Task ForDockerContainer_ReturnsLowercaseTruncatedTo63()
    {
        var key = IsolationKey.FromName("VERY.LONG.NAMESPACE.VeryLongTestClassName.VeryLongTestMethodName");

        var dockerName = key.ForDockerContainer();

        await Assert.That(dockerName).IsEqualTo(dockerName.ToLowerInvariant());
        await Assert.That(dockerName.Length).IsLessThanOrEqualTo(63);
    }

    [Test]
    public async Task ForRedisKeyPrefix_Truncates_To64Chars()
    {
        var longName = new string('a', 200);

        var prefix = IsolationKey.FromName(longName).ForRedisKeyPrefix();

        await Assert.That(prefix.Length).IsLessThanOrEqualTo(64);
    }
}
