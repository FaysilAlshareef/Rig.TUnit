using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Builder;

public sealed class CosmosRigBuilder : NoSqlRigBuilder<CosmosRigBuilder>
{
    public CosmosRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }

    public string AccountEndpoint => Source.ConnectionString;
}
