using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Builder;

public sealed class KurrentDbRigBuilder : NoSqlRigBuilder<KurrentDbRigBuilder>
{
    public KurrentDbRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
