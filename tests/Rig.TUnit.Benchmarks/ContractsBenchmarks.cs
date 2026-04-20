using BenchmarkDotNet.Attributes;
using Rig.TUnit.Microservices.Contracts;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class ContractsBenchmarks
{
    [Benchmark]
    public ContractPact Construct_ContractPact()
        => new("Consumer", "Provider", Array.Empty<ContractInteraction>());
}
