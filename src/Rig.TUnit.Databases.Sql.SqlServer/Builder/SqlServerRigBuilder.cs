using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;

namespace Rig.TUnit.Databases.Sql.SqlServer.Builder;

/// <summary>
/// SQL Server provider builder. Inherits <see cref="SqlRigBuilder{TSelf}"/> so
/// <c>ReplaceDbContext&lt;TContext&gt;</c> and base fluent APIs work without reimplementation.
/// </summary>
public sealed class SqlServerRigBuilder : SqlRigBuilder<SqlServerRigBuilder>
{
    public SqlServerRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    protected override void UseProvider(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString);
    }
}
