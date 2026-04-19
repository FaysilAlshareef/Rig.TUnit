using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;

namespace Rig.TUnit.Security.Policies.Builder;

public sealed class PolicyRigBuilder : SecurityRigBuilder<PolicyRigBuilder>
{
    public PolicyRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string Scheme => Source.ConnectionString;
}
