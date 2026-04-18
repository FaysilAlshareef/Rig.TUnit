using Rig.TUnit.Databases.NoSql.ElasticSearch.Assertions;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

/// <summary>
/// T034-RED unit tests for <see cref="DslAssert"/>. ElasticsearchClient is sealed and
/// transport-coupled — the unit suite covers null/empty-guards; live hit-count behaviour
/// is in <c>DslAssertLiveTests</c> in the Tests.Integration project.
/// </summary>
public sealed class DslAssertTests
{
    [Test]
    public async Task HitCountAsync_NullClient_Throws()
    {
        await Assert.That(async () => await DslAssert.HitCountAsync<object>(null!, "orders", _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HitCountAsync_EmptyIndex_Throws()
    {
        var client = TestClients.Offline();
        await Assert.That(async () => await DslAssert.HitCountAsync<object>(client, string.Empty, _ => { }))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task HitCountAsync_NullConfigure_Throws()
    {
        var client = TestClients.Offline();
        await Assert.That(async () => await DslAssert.HitCountAsync<object>(client, "orders", null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
