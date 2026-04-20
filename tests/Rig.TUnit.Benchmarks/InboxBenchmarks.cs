using BenchmarkDotNet.Attributes;
using Rig.TUnit.Microservices.Inbox;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class InboxBenchmarks
{
    private SequenceTracker _tracker = null!;
    private long _seq;

    [GlobalSetup]
    public void Setup() => _tracker = new SequenceTracker();

    [Benchmark]
    public bool TryApply_IncreasingSequence() => _tracker.TryApply("agg", Interlocked.Increment(ref _seq));
}
