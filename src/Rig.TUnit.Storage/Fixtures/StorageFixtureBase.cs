using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Storage.Contracts;

namespace Rig.TUnit.Storage.Fixtures;

public abstract class StorageFixtureBase : RigFixtureBase, IStorageRig
{
    protected StorageFixtureBase() { IsolationKey = IsolationKey.FromExecutionContext(); }
    public IsolationKey IsolationKey { get; }
    public virtual string ContainerName => IsolationKey.ForRedisKeyPrefix();
}
