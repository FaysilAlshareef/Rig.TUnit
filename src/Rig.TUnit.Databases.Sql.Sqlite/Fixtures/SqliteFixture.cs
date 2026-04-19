using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.Fixtures;
using Rig.TUnit.Databases.Sql.Sqlite.Options;

namespace Rig.TUnit.Databases.Sql.Sqlite.Fixtures;

/// <summary>
/// Real SQLite fixture using a shared-cache in-memory database. Owns a single
/// <see cref="SqliteConnection"/> that MUST stay open for the fixture lifetime,
/// otherwise the in-memory database is destroyed.
/// </summary>
public sealed class SqliteFixture : SqlFixtureBase
{
    private readonly SqliteFixtureOptions _options;
    private SqliteConnection? _keepAlive;
    private string? _connectionString;

    public SqliteFixture() : this(new SqliteFixtureOptions())
    {
    }

    public SqliteFixture(IOptions<SqliteFixtureOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public SqliteFixture(SqliteFixtureOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public override string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("InitializeAsync must run first");

    public override string DatabaseName => $"{_options.DatabaseName}_{IsolationKey.Value}";

    public override async Task InitializeAsync()
    {
        if (_connectionString is not null)
        {
            return;
        }

        _connectionString = $"Data Source={DatabaseName};Mode=Memory;Cache={_options.CacheMode}";
        _keepAlive = new SqliteConnection(_connectionString);
        await _keepAlive.OpenAsync().ConfigureAwait(false);

        if (_options.ForeignKeys)
        {
            await using var cmd = _keepAlive.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_keepAlive is not null)
        {
            await _keepAlive.DisposeAsync().ConfigureAwait(false);
            _keepAlive = null;
        }
        _connectionString = null;
    }
}
