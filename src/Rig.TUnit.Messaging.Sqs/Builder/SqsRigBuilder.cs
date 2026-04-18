using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;

namespace Rig.TUnit.Messaging.Sqs.Builder;

public sealed class SqsRigBuilder : MessagingRigBuilder<SqsRigBuilder>
{
    public SqsRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string ConnectionString => Source.ConnectionString;
}
