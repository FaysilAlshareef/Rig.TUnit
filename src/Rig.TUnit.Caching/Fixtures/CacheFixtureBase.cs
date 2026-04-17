using Rig.TUnit.Caching.Contracts;
using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;

namespace Rig.TUnit.Caching.Fixtures;

public abstract class CacheFixtureBase : RigFixtureBase, ICacheRig
{
    protected CacheFixtureBase()
    {
        IsolationKey = IsolationKey.FromExecutionContext();
    }

    public IsolationKey IsolationKey { get; }

    public virtual string KeyPrefix => IsolationKey.ForRedisKeyPrefix();
}
