using Elastic.Clients.Elasticsearch;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration;

public sealed class IndexRefreshLiveTests
{
    private static ElasticsearchClient NewClient(string connectionString)
        => new(new Uri(connectionString));

    [Test]
    public async Task RefreshAsync_ExistingIndex_Succeeds()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var client = NewClient(fx.ConnectionString);
        var indexName = $"idx_{Guid.NewGuid():N}";
        try
        {
            var createResponse = await client.Indices.CreateAsync(indexName);
            await Assert.That(createResponse.IsValidResponse).IsTrue();

            // Act + Assert
            await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(client, indexName))
                .ThrowsNothing();
        }
        finally
        {
            await client.Indices.DeleteAsync(indexName);
        }
    }

    [Test]
    public async Task RefreshAsync_UnknownIndex_Throws()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var client = NewClient(fx.ConnectionString);
        var missing = $"idx_missing_{Guid.NewGuid():N}";

        // Act + Assert
        await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(client, missing))
            .ThrowsExactly<InvalidOperationException>();
    }
}
