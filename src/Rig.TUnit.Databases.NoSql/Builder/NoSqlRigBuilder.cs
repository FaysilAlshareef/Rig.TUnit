using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Builder;

namespace Rig.TUnit.Databases.NoSql.Builder;

public abstract class NoSqlRigBuilder<TSelf> : DatabaseRigBuilder<TSelf>
    where TSelf : NoSqlRigBuilder<TSelf>
{
    protected NoSqlRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }
}
