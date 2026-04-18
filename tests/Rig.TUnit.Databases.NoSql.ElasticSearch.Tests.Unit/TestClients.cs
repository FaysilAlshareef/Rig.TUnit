using Elastic.Clients.Elasticsearch;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

/// <summary>
/// Helper that constructs an <see cref="ElasticsearchClient"/> pointed at a reserved
/// unreachable address. Used by guard-only unit tests — the clients never issue a
/// network call because the helpers short-circuit on argument validation first.
/// </summary>
internal static class TestClients
{
    public static ElasticsearchClient Offline()
        // RFC-5737 TEST-NET-1 — guaranteed non-routable.
        => new(new Uri("http://192.0.2.1:9200"));
}
