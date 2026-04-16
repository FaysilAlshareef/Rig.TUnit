using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;
using Rig.TUnit.Core.Fakers;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class CoreBenchmarks
{
    private ServiceCollection _services10 = null!;
    private ServiceCollection _services100 = null!;
    private ServiceCollection _services1000 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _services10 = CreateServicesWithDisposables(10);
        _services100 = CreateServicesWithDisposables(100);
        _services1000 = CreateServicesWithDisposables(1000);
    }

    [Benchmark]
    public object FakerGenerate()
    {
        var faker = new CustomConstructorFaker<BenchmarkEntity>();
        return faker.Generate();
    }

    [Benchmark]
    [BenchmarkDotNet.Attributes.Arguments(10)]
    [BenchmarkDotNet.Attributes.Arguments(100)]
    [BenchmarkDotNet.Attributes.Arguments(1000)]
    public IServiceCollection RemoveByName(int size)
    {
        var source = size switch
        {
            10 => _services10,
            100 => _services100,
            _ => _services1000,
        };
        var services = new ServiceCollection();
        foreach (var descriptor in source) ((IServiceCollection)services).Add(descriptor);
        return services.RemoveByName("Disposable");
    }

    private static ServiceCollection CreateServicesWithDisposables(int count)
    {
        var services = new ServiceCollection();
        for (var i = 0; i < count; i++)
        {
            services.AddSingleton<IDisposable>(_ => null!);
        }
        return services;
    }
}

public sealed class BenchmarkEntity
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    private BenchmarkEntity() { }
}
