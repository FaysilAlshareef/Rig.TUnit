using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;

namespace Rig.TUnit.Databases.Sql.Postgresql.Builder;

public sealed class PostgresRigBuilder : SqlRigBuilder<PostgresRigBuilder>
{
    public PostgresRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }

    protected override void UseProvider(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseNpgsql(connectionString);
    }
}
