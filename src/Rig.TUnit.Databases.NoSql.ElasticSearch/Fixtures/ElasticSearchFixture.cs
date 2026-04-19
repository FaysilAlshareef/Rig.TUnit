using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Options;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Testcontainers.Elasticsearch;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Fixtures;

public sealed class ElasticSearchFixture : DocumentFixtureBase
{
    private readonly ElasticSearchFixtureOptions _options;
    private ElasticsearchContainer? _container;
    private ElasticsearchClient? _client;

    public ElasticSearchFixture() : this(new ElasticSearchFixtureOptions()) { }

    public ElasticSearchFixture(IOptions<ElasticSearchFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public ElasticSearchFixture(ElasticSearchFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public ElasticsearchClient Client => _client
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;
        _container = new ElasticsearchBuilder(
                $"docker.elastic.co/elasticsearch/elasticsearch:{_options.ImageTag}")
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);

        // ES 8.x containers ship with self-signed HTTPS certs — the URI includes basic-auth
        // creds; we wire an always-true server-cert callback so dev certs validate.
        var settings = new ElasticsearchClientSettings(new Uri(_container.GetConnectionString()))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
        _client = new ElasticsearchClient(settings);
    }

    public override async ValueTask DisposeAsync()
    {
        _client = null;
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }
    }
}
