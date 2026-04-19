using BenchmarkDotNet.Attributes;
using Rig.TUnit.Storage.AzureBlob.Helpers;
using Rig.TUnit.Storage.AzureBlob.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class AzureBlobBenchmarks
{
    [Benchmark]
    public AzureBlobFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public AzureBlobFixtureOptions Options_ConstructWithOverrides() => new() { ImageTag = "3.30.0", StartupTimeoutSeconds = 60 };

    [Benchmark]
    public string SasBuilder_BuildQueryString()
        => AzureBlobSasBuilder.BuildQueryString("bucket", "key", "r", TimeSpan.FromMinutes(5), TimeProvider.System);
}
