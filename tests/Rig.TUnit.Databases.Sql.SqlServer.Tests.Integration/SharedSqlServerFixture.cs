using Rig.TUnit.Databases.Sql.SqlServer.Fixtures;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Integration;

/// <summary>
/// Process-wide shared SQL Server fixture so the MSSQL container boots once across
/// every test class in this assembly (Contract, DbContextHelper, Quirks). Disposal
/// piggybacks on process exit — integration tests run in short-lived test hosts, so
/// the Testcontainers SDK cleans up automatically via the Ryuk reaper.
/// </summary>
internal static class SharedSqlServerFixture
{
    private static readonly Lazy<Task<SqlServerFixture>> Instance = new(async () =>
    {
        var fx = new SqlServerFixture();
        await fx.InitializeAsync();
        return fx;
    });

    public static Task<SqlServerFixture> GetAsync() => Instance.Value;

    public static bool IsInitialized => Instance.IsValueCreated;

    public static async Task DisposeIfCreatedAsync()
    {
        if (Instance.IsValueCreated)
        {
            var fx = await Instance.Value.ConfigureAwait(false);
            await fx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
