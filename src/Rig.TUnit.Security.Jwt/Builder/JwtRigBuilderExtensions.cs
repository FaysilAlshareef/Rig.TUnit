using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Security.Jwt.Builder;

public static class JwtRigBuilderExtensions
{
    public static RigBuilder UseJwt(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<JwtRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new JwtRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
