using Rig.TUnit.Core;
using Rig.TUnit.Parallelism.Tests.Contract;

namespace Rig.TUnit.Messaging.Kafka.Tests.Integration;

[InheritsTests]
public sealed class KafkaParallelContract : ParallelIsolationContract
{
    protected override ValueTask<IParallelRig> CreateRigAsync(CancellationToken ct)
        => ValueTask.FromResult<IParallelRig>(new ParallelRigAdapter(IsolationKey.FromName(Guid.NewGuid().ToString())));
}
