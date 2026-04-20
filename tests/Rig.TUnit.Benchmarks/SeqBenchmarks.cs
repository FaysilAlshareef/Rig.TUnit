using BenchmarkDotNet.Attributes;
using Rig.TUnit.Observability.Seq.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class SeqBenchmarks
{
    [Benchmark]
    public SeqFixtureOptions Construct_DefaultOptions() => new();
}
