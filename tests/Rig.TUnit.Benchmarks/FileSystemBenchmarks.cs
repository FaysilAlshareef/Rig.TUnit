using BenchmarkDotNet.Attributes;
using Rig.TUnit.Storage.FileSystem.Helpers;
using Rig.TUnit.Storage.FileSystem.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class FileSystemBenchmarks
{
    private static readonly string Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rigtunit-bench");

    [Benchmark]
    public FileSystemFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public FileSystemFixtureOptions Options_ConstructWithOverrides() => new() { RootPathPrefix = "custom", CleanupOnDispose = false };

    [Benchmark]
    public string PathSandboxHelper_Resolve() => PathSandboxHelper.Resolve(Root, "sub/file.txt");

    [Benchmark]
    public bool PathSandboxHelper_IsInside() => PathSandboxHelper.IsInside(Root, System.IO.Path.Combine(Root, "a", "b.txt"));
}
