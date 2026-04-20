using BenchmarkDotNet.Attributes;
using Rig.TUnit.Ci.Enrichers;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures per-call overhead of <see cref="FlakyQuarantine"/> recording and
/// <see cref="CoverageDeltaEnforcer"/> decisions. Both are hot-path in multi-thousand-run
/// CI aggregations.
/// </summary>
[MemoryDiagnoser]
public class CiBenchmarks
{
    private FlakyQuarantine _quarantine = null!;
    private CoverageDeltaEnforcer _enforcer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quarantine = new FlakyQuarantine();
        _enforcer = new CoverageDeltaEnforcer(Minimum: 0.02);
    }

    [Benchmark]
    public void FlakyQuarantine_RecordFailure()
    {
        _quarantine.RecordFailure("Sample.Test");
    }

    [Benchmark]
    public bool CoverageDeltaEnforcer_IsAcceptable()
    {
        return _enforcer.IsAcceptable(0.88, 0.90);
    }
}
