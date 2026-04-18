using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Security.Contracts;

namespace Rig.TUnit.Security.Fixtures;

public abstract class SecurityFixtureBase : RigFixtureBase, ISecurityRig
{
    protected SecurityFixtureBase()
    {
        IsolationKey = IsolationKey.FromExecutionContext();
    }

    public IsolationKey IsolationKey { get; }
}
