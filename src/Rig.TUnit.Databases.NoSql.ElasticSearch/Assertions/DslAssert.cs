using Elastic.Clients.Elasticsearch;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Assertions;

/// <summary>
/// Thin DSL-assertion facade for ElasticSearch. Currently exposes
/// <see cref="HitCountAsync{T}"/> — builds a strongly-typed search request and returns
/// the hit total as a simple <c>long</c>. Add more fluent entries here as the contract
/// grows (e.g. aggregation bucket counts, top-hit IDs).
/// </summary>
public static class DslAssert
{
    public static async Task<long> HitCountAsync<T>(
        ElasticsearchClient client,
        string indexName,
        Action<SearchRequestDescriptor<T>> configure,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentException("indexName is required.", nameof(indexName));
        }
        ArgumentNullException.ThrowIfNull(configure);

        var response = await client.SearchAsync<T>(
            s =>
            {
                s.Indices(indexName);
                configure(s);
            },
            ct).ConfigureAwait(false);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Search failed on '{indexName}': {response.DebugInformation}");
        }

        return response.Total;
    }
}
