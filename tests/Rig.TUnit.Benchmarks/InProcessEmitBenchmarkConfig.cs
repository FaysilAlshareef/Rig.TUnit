using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Benchmark config for the 21 provider classes that ride <see cref="InProcessEmitToolchain"/>.
/// The toolchain avoids BDN's external auto-generated boilerplate build (which exceeds
/// the default 2-minute timeout on this 100+ project transitive graph).
///
/// Run shape mirrors <see cref="CiBenchmarkConfig"/> (3 warmup × 5 iterations × 1 launch,
/// Throughput strategy) so the in-process and out-of-process suites converge on the same
/// measurement variance budget. The previous <see cref="Job.Dry"/> + <see cref="RunStrategy.ColdStart"/>
/// + IterationCount=1 + InvocationCount=1 shape produced single-invocation cold-start JIT
/// timings (~0.5–1 ms per "Options_Construct*" call) with ~50 % run-to-run variance, which
/// dominated the false-positive alerts on the 2026-04-26 RCA run.
/// </summary>
public sealed class InProcessEmitBenchmarkConfig : ManualConfig
{
    public InProcessEmitBenchmarkConfig()
    {
        AddJob(Job.Default
            .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core90)
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithStrategy(RunStrategy.Throughput)
            .WithWarmupCount(3)
            .WithIterationCount(5)
            .WithLaunchCount(1));
    }
}
