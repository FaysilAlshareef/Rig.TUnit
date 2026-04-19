using Rig.TUnit.Caching.Builder;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Memory.Builder;

public sealed class MemoryCacheRigBuilder : CacheRigBuilder<MemoryCacheRigBuilder>
{
    public MemoryCacheRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }
}

internal sealed class InMemoryConnectionSource : IRigConnectionSource
{
    public string ConnectionString => "in-memory";
}

public static class MemoryCacheRigBuilderExtensions
{
    public static RigBuilder UseMemoryCache(this RigBuilder builder, Action<MemoryCacheRigBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var source = new InMemoryConnectionSource();
        var sub = new MemoryCacheRigBuilder(builder, source);
        configure?.Invoke(sub);
        return builder;
    }
}
