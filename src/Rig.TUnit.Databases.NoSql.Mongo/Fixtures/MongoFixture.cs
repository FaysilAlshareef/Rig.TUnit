using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Rig.TUnit.Databases.NoSql.Fixtures;
using Rig.TUnit.Databases.NoSql.Mongo.Options;
using Testcontainers.MongoDb;

namespace Rig.TUnit.Databases.NoSql.Mongo.Fixtures;

public sealed class MongoFixture : DocumentFixtureBase
{
    private readonly MongoFixtureOptions _options;
    private MongoDbContainer? _container;
    private IMongoClient? _client;

    public MongoFixture() : this(new MongoFixtureOptions()) { }

    public MongoFixture(IOptions<MongoFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value) { }

    public MongoFixture(MongoFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string DatabaseName => IsolationKey.ForPostgresDatabase();

    public IMongoDatabase Database => (_client ?? throw new InvalidOperationException("not initialised"))
        .GetDatabase(DatabaseName);

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        _container = new MongoDbBuilder()
            .WithImage($"mongo:{_options.ImageTag}")
            .WithUsername(_options.Username)
            .WithPassword(_options.Password)
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.StartupTimeoutSeconds));
        await _container.StartAsync(cts.Token).ConfigureAwait(false);
        _client = new MongoClient(_container.GetConnectionString());
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
            _client = null;
        }
    }
}
