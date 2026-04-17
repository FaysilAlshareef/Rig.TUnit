using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Builder;

public abstract class CacheRigBuilder<TSelf> where TSelf : CacheRigBuilder<TSelf>
{
    protected CacheRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    protected RigBuilder Root { get; }
    protected IRigConnectionSource Source { get; }

    public RigBuilder And() => Root;
}
