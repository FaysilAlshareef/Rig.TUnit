using Rig.TUnit.Core;

namespace Rig.TUnit.Parallelism.Tests.Contract;

/// <summary>Minimal contract every parallel-tested rig exposes.</summary>
public interface IParallelRig : IAsyncDisposable
{
    IsolationKey IsolationKey { get; }
}
