using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Fixtures;

/// <summary>
/// Abstract base for every database fixture. Concrete fixtures override
/// <see cref="RigFixtureBase.InitializeAsync"/> / <see cref="RigFixtureBase.DisposeAsync"/>
/// and expose a provider-specific connection string.
/// </summary>
public abstract class DbFixtureBase : RigFixtureBase, IDbRig
{
    protected DbFixtureBase()
    {
        IsolationKey = IsolationKey.FromExecutionContext();
    }

    public IsolationKey IsolationKey { get; }

    public abstract string DatabaseName { get; }
}
