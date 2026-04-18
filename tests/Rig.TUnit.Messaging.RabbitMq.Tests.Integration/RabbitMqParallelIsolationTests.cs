using Rig.TUnit.Core;
using Rig.TUnit.Parallelism.Tests.Contract;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Integration;

[InheritsTests]
public sealed class RabbitMqParallelContract : ParallelIsolationContract
{
    protected override ValueTask<IParallelRig> CreateRigAsync(CancellationToken ct)
        => ValueTask.FromResult<IParallelRig>(new ParallelRigAdapter(IsolationKey.FromName(Guid.NewGuid().ToString())));
}
