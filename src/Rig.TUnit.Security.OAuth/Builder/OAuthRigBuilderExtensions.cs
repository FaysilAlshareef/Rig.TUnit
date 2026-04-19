using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Security.OAuth.Builder;

public static class OAuthRigBuilderExtensions
{
    public static RigBuilder UseOAuthServer(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<OAuthRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OAuthRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
