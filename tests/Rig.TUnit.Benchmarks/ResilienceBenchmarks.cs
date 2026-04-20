using BenchmarkDotNet.Attributes;
using Rig.TUnit.Resilience;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class ResilienceBenchmarks
{
    private ResilienceClock _clock = null!;

    [GlobalSetup]
    public void Setup() => _clock = new ResilienceClock(DateTimeOffset.UtcNow);

    [Benchmark]
    public void ResilienceClock_Advance() => _clock.Advance(TimeSpan.FromMilliseconds(1));
}
