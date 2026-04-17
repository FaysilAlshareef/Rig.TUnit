using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Messaging.Contracts;
using Rig.TUnit.Messaging.Conventions;

namespace Rig.TUnit.Messaging.Fixtures;

public abstract class MessagingFixtureBase : RigFixtureBase, IMessagingRig
{
    protected MessagingFixtureBase()
    {
        IsolationKey = IsolationKey.FromExecutionContext();
    }

    public IsolationKey IsolationKey { get; }

    public virtual string TopicName => TopicNamingConvention.Default(IsolationKey);
}
