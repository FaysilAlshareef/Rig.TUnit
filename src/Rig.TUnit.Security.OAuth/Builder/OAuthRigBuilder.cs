using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;

namespace Rig.TUnit.Security.OAuth.Builder;

public sealed class OAuthRigBuilder : SecurityRigBuilder<OAuthRigBuilder>
{
    public OAuthRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string Issuer => Source.ConnectionString;
}
