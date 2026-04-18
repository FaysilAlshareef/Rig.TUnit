using Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

/// <summary>
/// T034-RED unit tests for <see cref="IndexRefreshHelper"/>. ElasticsearchClient is
/// sealed with only static transport plumbing — the unit suite covers the pure
/// null/empty-guards on public inputs; live end-to-end refresh is covered by
/// <c>IndexRefreshLiveTests</c> in the Tests.Integration project.
/// </summary>
public sealed class IndexRefreshHelperTests
{
    [Test]
    public async Task RefreshAsync_NullClient_Throws()
    {
        await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(null!, "orders"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RefreshAsync_EmptyIndex_Throws()
    {
        // Use a concrete but unreachable client — we expect the guard to fire before any network call.
        var client = TestClients.Offline();
        await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(client, string.Empty))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task RefreshAsync_NullIndex_Throws()
    {
        var client = TestClients.Offline();
        await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(client, null!))
            .ThrowsExactly<ArgumentException>();
    }
}
