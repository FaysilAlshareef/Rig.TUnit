using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Storage.Builder;

public abstract class StorageRigBuilder<TSelf> where TSelf : StorageRigBuilder<TSelf>
{
    protected StorageRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }
    protected RigBuilder Root { get; }
    protected IRigConnectionSource Source { get; }
    public RigBuilder And() => Root;
}
