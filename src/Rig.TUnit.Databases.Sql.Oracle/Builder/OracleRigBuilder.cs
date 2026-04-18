using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;

namespace Rig.TUnit.Databases.Sql.Oracle.Builder;

public sealed class OracleRigBuilder : SqlRigBuilder<OracleRigBuilder>
{
    public OracleRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }

    protected override void UseProvider(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseOracle(connectionString);
    }
}
