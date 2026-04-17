using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.Databases.Sql.Assertions;

/// <summary>
/// Assertions over raw SQL executed by tests. The SQL strings here are authored by the
/// test (trusted input), NOT taken from user input — the <c>parameters</c> overloads
/// accept <see cref="System.Data.Common.DbParameter"/> values for any runtime data
/// that must be bound safely. Never pass user-provided SQL to these methods.
/// </summary>
public static class RawSqlAssert
{
    /// <summary>Asserts the query returns the expected scalar value.</summary>
    public static async Task Returns<T>(DbContext context, string sql, T expected, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var raw = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        var actual = raw is null || raw is DBNull
            ? default
            : (T)Convert.ChangeType(raw, typeof(T), System.Globalization.CultureInfo.InvariantCulture);

        if (!Equals(actual, expected))
        {
            throw new InvalidOperationException($"RawSqlAssert.Returns: expected '{expected}' got '{actual}'");
        }
    }

    /// <summary>Asserts the non-query SQL affected exactly <paramref name="expectedRows"/> rows.</summary>
    public static async Task Affects(DbContext context, string sql, int expectedRows, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        // ExecuteSqlRawAsync here accepts a parameter array via the overload when the
        // test needs runtime-bound values; the SQL template itself is always test-authored.
        var affected = await context.Database.ExecuteSqlRawAsync(sql, ct).ConfigureAwait(false);
        if (affected != expectedRows)
        {
            throw new InvalidOperationException($"RawSqlAssert.Affects: expected {expectedRows} rows, got {affected}");
        }
    }
}
