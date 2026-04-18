using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Extensions;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// T176a retroactive benchmark coverage for the Postgres fluent entry points. Measures
/// allocation of the RigBuilder fluent chain and the EF Core wrapper call. Does NOT start
/// a Postgres container — pure wiring path only. Serves as the 4th leg (Unit / Integration
/// / Contract / Benchmark) the TDD Gate mandates for every provider.
/// </summary>
[MemoryDiagnoser]
public class PostgresUseBenchmarks
{
    private const string ConnectionString = "Host=localhost;Database=test;Username=u;Password=p";
    private IRigConnectionSource _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = RigConnect.FromValue(ConnectionString);
    }

    [Benchmark]
    public RigBuilder UsePostgres_FluentChain()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig =>
        {
            captured = rig;
            rig.UsePostgres(_source, _ => { });
        });
        return captured!;
    }

    [Benchmark]
    public DbContextOptions UsePostgres_DbContextOptions_Generic()
    {
        return new DbContextOptionsBuilder<DummyDbContext>()
            .UsePostgres(ConnectionString)
            .Options;
    }

    [Benchmark]
    public DbContextOptions UsePostgres_DbContextOptions_NonGeneric()
    {
        return new DbContextOptionsBuilder()
            .UsePostgres(ConnectionString)
            .Options;
    }

    private sealed class DummyDbContext(DbContextOptions<DummyDbContext> options) : DbContext(options);
}
