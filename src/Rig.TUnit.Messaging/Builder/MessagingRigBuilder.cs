using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Messaging.Builder;

/// <summary>
/// Shared root for every messaging rig builder. Deliberately does NOT declare a generic
/// <c>WithTopology</c> method.
/// </summary>
/// <remarks>
/// Per <c>C-003</c> (see Feature 007 spec), each <c>{Provider}RigBuilder</c> — e.g.
/// <c>ServiceBusRigBuilder</c>, <c>KafkaRigBuilder</c>, <c>SqsRigBuilder</c> — declares its
/// own strongly-typed <c>WithTopology(Action&lt;I{Provider}TopologyBuilder&gt;)</c> against a
/// provider-specific sub-interface. A generic overload on this base class would re-introduce
/// the runtime-unsupported trap the design was created to prevent.
/// </remarks>
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
