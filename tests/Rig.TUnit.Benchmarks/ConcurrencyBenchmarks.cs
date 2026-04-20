using BenchmarkDotNet.Attributes;
using Rig.TUnit.Concurrency.Assertions;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures per-call overhead of the concurrency helpers' hot paths.
/// </summary>
[MemoryDiagnoser]
public class ConcurrencyBenchmarks
{
    private SequenceIdempotencyChecker _checker = null!;

    [GlobalSetup]
    public void Setup()
    {
        _checker = new SequenceIdempotencyChecker();
    }

    [Benchmark]
    public bool SequenceIdempotencyChecker_TryApply()
    {
        return _checker.TryApply("agg", Random.Shared.NextInt64());
    }
}
