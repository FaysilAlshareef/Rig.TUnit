using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Security.Mtls.Builder;

public static class MtlsRigBuilderExtensions
{
    public static RigBuilder UseMtls(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<MtlsRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new MtlsRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
