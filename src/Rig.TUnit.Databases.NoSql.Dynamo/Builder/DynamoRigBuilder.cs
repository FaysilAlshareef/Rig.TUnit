using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Builder;

public sealed class DynamoRigBuilder : NoSqlRigBuilder<DynamoRigBuilder>
{
    public DynamoRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
