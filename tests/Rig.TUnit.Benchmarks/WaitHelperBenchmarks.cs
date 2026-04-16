using BenchmarkDotNet.Attributes;
using Rig.TUnit.Core.Helpers;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class WaitHelperBenchmarks
{
    [Benchmark]
    public async Task WaitFor_ConditionTrueImmediately()
    {
        await WaitHelper.WaitForAsync(() => true, TimeSpan.FromSeconds(1));
    }

    [Benchmark]
    public async Task WaitFor_AsyncConditionTrueImmediately()
    {
        await WaitHelper.WaitForAsync(() => Task.FromResult(true), TimeSpan.FromSeconds(1));
    }

    [Benchmark]
    public async Task<string> WaitForResult_NonNull()
    {
        return await WaitHelper.WaitForResultAsync<string>(
            () => Task.FromResult<string?>("result"),
            TimeSpan.FromSeconds(1));
    }
}
