using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Builder;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Builder;

public sealed class ElasticSearchRigBuilder : NoSqlRigBuilder<ElasticSearchRigBuilder>
{
    public ElasticSearchRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
