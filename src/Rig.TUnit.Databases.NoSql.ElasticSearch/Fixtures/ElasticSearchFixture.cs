using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.Elasticsearch;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Fixtures;

public sealed class ElasticSearchFixture : DocumentFixtureBase
{
    private ElasticsearchContainer? _container;

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new ElasticsearchBuilder()
            .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.15.3")
            .Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(6));
        await _container.StartAsync(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
