using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class SqlServerBenchmarks
{
    private IServiceScopeFactory _scopeFactory = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddDbContext<BenchmarkDbContext>(options =>
            options.UseInMemoryDatabase($"bench_{Guid.NewGuid():N}"));
        _scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    [Benchmark]
    public object ScopeCreation()
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
    }
}

public sealed class BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : DbContext(options);
