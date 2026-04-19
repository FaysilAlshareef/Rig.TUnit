using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Security.Policies.Builder;

public static class PolicyRigBuilderExtensions
{
    public static RigBuilder UsePolicies(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<PolicyRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new PolicyRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
