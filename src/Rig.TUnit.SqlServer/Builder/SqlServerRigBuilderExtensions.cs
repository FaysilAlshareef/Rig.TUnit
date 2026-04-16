using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.SqlServer.Builder;

/// <summary>
/// Entry-point extensions that wire SQL Server testing support into a <see cref="RigBuilder"/>.
/// </summary>
public static class SqlServerRigBuilderExtensions
{
    /// <summary>
    /// Registers SQL Server test infrastructure scoped to the supplied connection source.
    /// </summary>
    public static RigBuilder UseSqlServer(
        this RigBuilder rigBuilder,
        IRigConnectionSource source,
        Action<SqlServerRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rigBuilder);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new SqlServerRigBuilder(rigBuilder.Services, source);
        configure(builder);
        return rigBuilder;
    }
}
