using Elastic.Clients.Elasticsearch;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Assertions;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration;

public sealed class DslAssertLiveTests
{
    private sealed record Doc(string Id, string Sku, int Qty);

    [Test]
    public async Task HitCountAsync_MatchingQuery_ReturnsIndexedCount()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var index = $"idx_{Guid.NewGuid():N}";
        try
        {
            await fx.Client.Indices.CreateAsync(index);
            await fx.Client.IndexAsync(new Doc("1", "sku-a", 2), i => i.Index(index));
            await fx.Client.IndexAsync(new Doc("2", "sku-a", 5), i => i.Index(index));
            await fx.Client.IndexAsync(new Doc("3", "sku-b", 1), i => i.Index(index));
            await IndexRefreshHelper.RefreshAsync(fx.Client, index);

            // Act
            var total = await DslAssert.HitCountAsync<Doc>(
                fx.Client,
                index,
                s => s.Query(q => q.Term(t => t.Field(new Field("sku.keyword")).Value("sku-a"))));

            // Assert
            await Assert.That(total).IsEqualTo(2L);
        }
        finally
        {
            await fx.Client.Indices.DeleteAsync(index);
        }
    }

    [Test]
    public async Task HitCountAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var index = $"idx_{Guid.NewGuid():N}";
        try
        {
            await fx.Client.Indices.CreateAsync(index);
            await fx.Client.IndexAsync(new Doc("1", "sku-a", 2), i => i.Index(index));
            await IndexRefreshHelper.RefreshAsync(fx.Client, index);

            // Act
            var total = await DslAssert.HitCountAsync<Doc>(
                fx.Client,
                index,
                s => s.Query(q => q.Term(t => t.Field(new Field("sku.keyword")).Value("sku-missing"))));

            // Assert
            await Assert.That(total).IsEqualTo(0L);
        }
        finally
        {
            await fx.Client.Indices.DeleteAsync(index);
        }
    }
}
