using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.Databases.Sql.Oracle.Extensions;

/// <summary>
/// EF Core wrapper convenience — thin alias over Oracle's <c>UseOracle</c>
/// so test authors write <c>options.UseOracle(fixture.ConnectionString)</c>
/// in lockstep with the <c>rig.UseOracle(...)</c> fluent entry point.
/// </summary>
public static class OracleBuilderExtensions
{
    public static DbContextOptionsBuilder UseOracle(
        this DbContextOptionsBuilder options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        return Microsoft.EntityFrameworkCore.OracleDbContextOptionsExtensions.UseOracle(options, connectionString);
    }

    public static DbContextOptionsBuilder<TContext> UseOracle<TContext>(
        this DbContextOptionsBuilder<TContext> options,
        string connectionString)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        Microsoft.EntityFrameworkCore.OracleDbContextOptionsExtensions.UseOracle(options, connectionString);
        return options;
    }
}
