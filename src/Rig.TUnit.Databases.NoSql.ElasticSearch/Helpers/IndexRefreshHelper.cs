using Elastic.Clients.Elasticsearch;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

/// <summary>
/// Forces an index refresh so freshly-indexed documents become searchable. Elasticsearch's
/// near-real-time default is ~1 s — tests that rely on a document being searchable on the
/// next line MUST call this after an index op. Throws when the index doesn't exist or the
/// refresh response reports a non-valid payload.
/// </summary>
public static class IndexRefreshHelper
{
    public static async Task RefreshAsync(
        ElasticsearchClient client,
        string indexName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentException("indexName is required.", nameof(indexName));
        }

        var response = await client.Indices.RefreshAsync(indexName, ct).ConfigureAwait(false);
        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Index refresh failed for '{indexName}': {response.DebugInformation}");
        }
    }
}
