using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.Builder;

public abstract class MessagingRigBuilder<TSelf> where TSelf : MessagingRigBuilder<TSelf>
{
    protected MessagingRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    protected RigBuilder Root { get; }
    protected IRigConnectionSource Source { get; }

    public RigBuilder And() => Root;
}
