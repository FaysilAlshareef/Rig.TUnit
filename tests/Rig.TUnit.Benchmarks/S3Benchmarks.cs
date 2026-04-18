using BenchmarkDotNet.Attributes;
using Rig.TUnit.Storage.S3.Helpers;
using Rig.TUnit.Storage.S3.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class S3Benchmarks
{
    [Benchmark]
    public S3FixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public S3FixtureOptions Options_ConstructWithOverrides() => new() { ImageTag = "3.4", StartupTimeoutSeconds = 120 };

    [Benchmark]
    public S3PresignRequest SasBuilder_BuildPresignRequest()
        => S3SasBuilder.BuildPresignRequest("bucket", "key", "GET", TimeSpan.FromMinutes(5), TimeProvider.System);
}
