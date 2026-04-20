using BenchmarkDotNet.Attributes;
using Rig.TUnit.HealthChecks.Assertions;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class HealthChecksBenchmarks
{
    private DependencyDownSimulator _sim = null!;

    [GlobalSetup]
    public void Setup() => _sim = new DependencyDownSimulator();

    [Benchmark]
    public bool DependencyDownSimulator_IsDown() => _sim.IsDown;

    [Benchmark]
    public void DependencyDownSimulator_Toggle()
    {
        _sim.GoDown();
        _sim.Recover();
    }
}
