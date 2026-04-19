using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;

namespace Rig.TUnit.Security.Jwt.Builder;

public sealed class JwtRigBuilder : SecurityRigBuilder<JwtRigBuilder>
{
    public JwtRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string Issuer => Source.ConnectionString;
}
