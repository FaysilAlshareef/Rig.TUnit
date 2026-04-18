using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Builder;

namespace Rig.TUnit.Security.Mtls.Builder;

public sealed class MtlsRigBuilder : SecurityRigBuilder<MtlsRigBuilder>
{
    public MtlsRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    public string Thumbprint => Source.ConnectionString;
}
