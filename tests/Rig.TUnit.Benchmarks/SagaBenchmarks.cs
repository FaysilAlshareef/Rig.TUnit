using BenchmarkDotNet.Attributes;
using Rig.TUnit.Microservices.Saga.Helpers;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class SagaBenchmarks
{
    [Benchmark]
    public bool SagaTimeoutHelper_HasTimedOut()
        => SagaTimeoutHelper.HasTimedOut(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(500));
}
