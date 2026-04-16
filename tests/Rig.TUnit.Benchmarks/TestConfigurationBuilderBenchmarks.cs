using BenchmarkDotNet.Attributes;
using Rig.TUnit.Core.Configuration;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class TestConfigurationBuilderBenchmarks
{
    [Benchmark]
    public object Build_TenKeys()
    {
        var builder = new TestConfigurationBuilder();
        for (var i = 0; i < 10; i++)
        {
            builder.Set($"Key{i}", $"Value{i}");
        }
        return builder.Build();
    }

    [Benchmark]
    public object Build_HundredKeys()
    {
        var builder = new TestConfigurationBuilder();
        for (var i = 0; i < 100; i++)
        {
            builder.Set($"Key{i}", $"Value{i}");
        }
        return builder.Build();
    }

    [Benchmark]
    public object Create_Static()
    {
        return TestConfigurationBuilder.Create(b => b
            .Set("Hello", "World")
            .Set("Number", "42"));
    }
}
