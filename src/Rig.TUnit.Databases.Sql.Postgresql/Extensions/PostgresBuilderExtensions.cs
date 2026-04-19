using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.Databases.Sql.Postgresql.Extensions;

/// <summary>
/// EF Core wrapper convenience — thin alias over <c>UseNpgsql</c> so test authors write
/// <c>options.UsePostgres(fixture.ConnectionString)</c> in lockstep with the
/// <c>rig.UsePostgres(...)</c> fluent entry point. Keeps the verb consistent between the
/// fixture-side and EF-side APIs without re-exposing Npgsql's full surface.
/// </summary>
public static class PostgresBuilderExtensions
{
    public static DbContextOptionsBuilder UsePostgres(
        this DbContextOptionsBuilder options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return options.UseNpgsql(connectionString);
    }

    public static DbContextOptionsBuilder<TContext> UsePostgres<TContext>(
        this DbContextOptionsBuilder<TContext> options,
        string connectionString)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        options.UseNpgsql(connectionString);
        return options;
    }
}
