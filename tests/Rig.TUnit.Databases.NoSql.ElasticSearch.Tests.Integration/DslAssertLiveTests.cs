using Elastic.Clients.Elasticsearch;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Assertions;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration;

public sealed class DslAssertLiveTests
{
    private sealed record Doc(string Id, string Sku, int Qty);

    private static ElasticsearchClient NewClient(string connectionString)
        => new(new Uri(connectionString));

    [Test]
    public async Task HitCountAsync_MatchingQuery_ReturnsIndexedCount()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var client = NewClient(fx.ConnectionString);
        var index = $"idx_{Guid.NewGuid():N}";
        try
        {
            await client.Indices.CreateAsync(index);
            await client.IndexAsync(new Doc("1", "sku-a", 2), i => i.Index(index));
            await client.IndexAsync(new Doc("2", "sku-a", 5), i => i.Index(index));
            await client.IndexAsync(new Doc("3", "sku-b", 1), i => i.Index(index));
            await IndexRefreshHelper.RefreshAsync(client, index);

            // Act
            var total = await DslAssert.HitCountAsync<Doc>(
                client,
                index,
                s => s.Query(q => q.Term(t => t.Field("sku.keyword").Value("sku-a"))));

            // Assert
            await Assert.That(total).IsEqualTo(2L);
        }
        finally
        {
            await client.Indices.DeleteAsync(index);
        }
    }

    [Test]
    public async Task HitCountAsync_NoMatches_ReturnsZero()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var client = NewClient(fx.ConnectionString);
        var index = $"idx_{Guid.NewGuid():N}";
        try
        {
            await client.Indices.CreateAsync(index);
            await client.IndexAsync(new Doc("1", "sku-a", 2), i => i.Index(index));
            await IndexRefreshHelper.RefreshAsync(client, index);

            // Act
            var total = await DslAssert.HitCountAsync<Doc>(
                client,
                index,
                s => s.Query(q => q.Term(t => t.Field("sku.keyword").Value("sku-missing"))));

            // Assert
            await Assert.That(total).IsEqualTo(0L);
        }
        finally
        {
            await client.Indices.DeleteAsync(index);
        }
    }
}
