using Rig.TUnit.Core;

namespace Rig.TUnit.Parallelism.Tests.Contract;

/// <summary>
/// Lightweight <see cref="IParallelRig"/> used by every provider's parallelism
/// contract binding. Wraps a pre-computed <see cref="IsolationKey"/> without
/// standing up an actual container — the contract is about proving unique isolation
/// keys under parallel execution, not about booting 20 concurrent containers.
/// </summary>
public sealed class ParallelRigAdapter(IsolationKey key) : IParallelRig
{
    public IsolationKey IsolationKey { get; } = key;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
