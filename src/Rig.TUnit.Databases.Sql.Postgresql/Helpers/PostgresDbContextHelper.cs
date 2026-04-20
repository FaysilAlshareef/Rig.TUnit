using Npgsql;

namespace Rig.TUnit.Databases.Sql.Postgresql.Helpers;

/// <summary>
/// Provider-specific helpers for Postgres-backed integration tests. The headline entry
/// point is <see cref="CreateEphemeralDatabaseAsync(string, CancellationToken)"/>: each
/// test calls it to obtain a freshly-created physical database on the same container
/// (hence the same host/port/credentials) and disposes it to drop the database.
///
/// Use per-test isolation whenever two or more tests in the same project materialise
/// schema — EF's <c>EnsureCreated</c>, raw DDL via <c>DbContextHelper</c>, or a sibling
/// fixture's reset logic. Sharing one physical database across parallel tests reopens
/// the class of race documented in [planning/post-004-remediation/CI-Postgres-Flake-RCA.md].
/// </summary>
public static class PostgresDbContextHelper
{
    /// <summary>
    /// Creates a uniquely-named database on the Postgres instance addressed by
    /// <paramref name="adminConnectionString"/> and returns an <see cref="EphemeralDatabase"/>
    /// wrapper. Disposing the wrapper terminates any remaining sessions against the database
    /// and runs <c>DROP DATABASE</c>.
    /// </summary>
    /// <param name="adminConnectionString">
    /// Connection string pointing at any existing database on the target Postgres instance
    /// (typically <c>postgres</c> — the default admin database handed out by Testcontainers).
    /// The new database is created with the same user; ensure that user has <c>CREATEDB</c>.
    /// </param>
    /// <param name="ct">Cancellation token propagated into all Npgsql calls.</param>
    /// <returns>
    /// A disposable wrapper whose <see cref="EphemeralDatabase.ConnectionString"/> addresses
    /// the newly-created database.
    /// </returns>
    public static async Task<EphemeralDatabase> CreateEphemeralDatabaseAsync(
        string adminConnectionString,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminConnectionString);

        // Postgres identifier cap is 63 bytes. `eph_` prefix + 32-char guid = 36 chars total.
        var databaseName = $"eph_{Guid.NewGuid():N}";

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync(ct).ConfigureAwait(false);
            await using var create = admin.CreateCommand();
            // Identifier is server-generated from a Guid — no untrusted input reaches the
            // command text. Npgsql does not parameterise DDL identifiers so we quote them.
            create.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await create.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }

        var ephemeralConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName,
        }.ConnectionString;

        return new EphemeralDatabase(adminConnectionString, databaseName, ephemeralConnectionString);
    }

    /// <summary>
    /// Handle to a per-test database. Exposes the connection string for consumers to feed
    /// into <see cref="Microsoft.EntityFrameworkCore.DbContextOptionsBuilder"/> or Npgsql
    /// directly, and drops the database on dispose.
    /// </summary>
    public sealed class EphemeralDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private bool _disposed;

        internal EphemeralDatabase(
            string adminConnectionString,
            string databaseName,
            string ephemeralConnectionString)
        {
            _adminConnectionString = adminConnectionString;
            DatabaseName = databaseName;
            ConnectionString = ephemeralConnectionString;
        }

        /// <summary>Server-generated unique database name.</summary>
        public string DatabaseName { get; }

        /// <summary>Connection string addressing the ephemeral database. Hand this to EF or Npgsql.</summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Forcefully terminates any remaining sessions against <see cref="DatabaseName"/> and
        /// drops the database. Idempotent.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            // Use a fresh connection to the admin DB — the per-test EF context may have left
            // an idle connection in the Npgsql pool against the ephemeral DB which would
            // block DROP DATABASE.
            NpgsqlConnection.ClearAllPools();

            await using var admin = new NpgsqlConnection(_adminConnectionString);
            await admin.OpenAsync().ConfigureAwait(false);

            await using (var terminate = admin.CreateCommand())
            {
                terminate.CommandText =
                    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity "
                    + "WHERE datname = @db AND pid <> pg_backend_pid()";
                terminate.Parameters.AddWithValue("db", DatabaseName);
                await terminate.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await using (var drop = admin.CreateCommand())
            {
                drop.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\"";
                await drop.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
