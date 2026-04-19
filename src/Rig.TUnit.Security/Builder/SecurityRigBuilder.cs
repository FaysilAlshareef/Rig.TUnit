using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Security.Builder;

public abstract class SecurityRigBuilder<TSelf> where TSelf : SecurityRigBuilder<TSelf>
{
    protected SecurityRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    protected RigBuilder Root { get; }
    protected IRigConnectionSource Source { get; }

    public RigBuilder And() => Root;
}
