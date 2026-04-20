using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Rig.TUnit.Databases.Sql.Sqlite.Fixtures;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures the startup cost of <see cref="SqliteFixture"/> + a trivial SELECT 1
/// round-trip — representative of a per-test ephemeral SQLite workflow.
/// </summary>
[MemoryDiagnoser]
public class SqliteBenchmarks
{
    [Benchmark]
    public async Task<object?> InitializeAndQuery()
    {
        await using var fixture = new SqliteFixture();
        await fixture.InitializeAsync();

        await using var conn = new SqliteConnection(fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1;";
        return await cmd.ExecuteScalarAsync();
    }
}
