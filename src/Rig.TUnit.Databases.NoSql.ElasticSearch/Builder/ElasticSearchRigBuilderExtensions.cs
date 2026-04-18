using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Builder;

public static class ElasticSearchRigBuilderExtensions
{
    public static RigBuilder UseElasticSearch(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<ElasticSearchRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ElasticSearchRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
