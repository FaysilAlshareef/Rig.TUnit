using Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Integration;

public sealed class IndexRefreshLiveTests
{
    [Test]
    public async Task RefreshAsync_ExistingIndex_Succeeds()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var indexName = $"idx_{Guid.NewGuid():N}";
        try
        {
            var createResponse = await fx.Client.Indices.CreateAsync(indexName);
            await Assert.That(createResponse.IsValidResponse).IsTrue();

            // Act + Assert
            await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(fx.Client, indexName))
                .ThrowsNothing();
        }
        finally
        {
            await fx.Client.Indices.DeleteAsync(indexName);
        }
    }

    [Test]
    public async Task RefreshAsync_UnknownIndex_Throws()
    {
        // Arrange
        var fx = await SharedElasticSearchFixture.GetAsync();
        var missing = $"idx_missing_{Guid.NewGuid():N}";

        // Act + Assert
        await Assert.That(async () => await IndexRefreshHelper.RefreshAsync(fx.Client, missing))
            .ThrowsExactly<InvalidOperationException>();
    }
}
