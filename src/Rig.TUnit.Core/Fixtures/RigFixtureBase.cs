using Rig.TUnit.Core.Builder;
using TUnit.Core.Interfaces;

namespace Rig.TUnit.Core.Fixtures;

/// <summary>
/// Base class for fixtures that manage async resources.
/// Implements TUnit's IAsyncInitializer + IAsyncDisposable lifecycle.
/// </summary>
public abstract class RigFixtureBase : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource
{
    public abstract string ConnectionString { get; }

    public abstract Task InitializeAsync();

    public abstract ValueTask DisposeAsync();
}
