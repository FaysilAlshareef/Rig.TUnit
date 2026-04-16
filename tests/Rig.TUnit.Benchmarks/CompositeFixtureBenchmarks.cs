using BenchmarkDotNet.Attributes;
using Rig.TUnit.Core.Fixtures;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class CompositeFixtureBenchmarks
{
    private sealed class NoopFixture { }

    [Benchmark]
    public object Get_FirstOfThree()
    {
        var a = new NoopFixture();
        var b = new object();
        var c = new object();
        var composite = new CompositeFixture(a, b, c);
        return composite.Get<NoopFixture>();
    }

    [Benchmark]
    public async Task InitializeAsync_NoInitializers()
    {
        var composite = new CompositeFixture(new object(), new object(), new object());
        await composite.InitializeAsync();
    }

    [Benchmark]
    public async Task DisposeAsync_NoDisposables()
    {
        var composite = new CompositeFixture(new object(), new object());
        await composite.DisposeAsync();
    }
}
