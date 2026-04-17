using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Extensions;
using Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Databases.Sql.SqlServer.Tests.Unit;

/// <summary>
/// Ported from the original <c>Rig.TUnit.SqlServer.Tests.Unit</c> suite. The old
/// <c>services.UseInMemoryDatabase&lt;T&gt;()</c> extension was relocated to
/// <c>Rig.TUnit.Databases.Sql</c> as <c>rig.UseInMemoryDb&lt;T&gt;(name)</c> on
/// <see cref="RigBuilder"/>. Tests adapted to the new surface while preserving intent.
/// </summary>
public sealed class InMemoryDbExtensionsTests
{
    private static IServiceProvider BuildWithInMemory(string databaseName, Action<IServiceCollection>? preConfigure = null)
    {
        var services = new ServiceCollection();
        services.AddRigTUnit(rig =>
        {
            preConfigure?.Invoke(rig.Services);
            rig.UseInMemoryDb<TestDbContext>(databaseName);
        });
        return services.BuildServiceProvider();
    }

    [Test]
    public async Task UseInMemoryDb_RegistersInMemoryProvider()
    {
        var provider = BuildWithInMemory("register-test");

        var context = provider.GetRequiredService<TestDbContext>();
        await Assert.That(context.Database.ProviderName).IsEqualTo("Microsoft.EntityFrameworkCore.InMemory");
    }

    [Test]
    public async Task UseInMemoryDb_TwoDistinctDatabaseNames_ProduceIsolatedStores()
    {
        var ctx1 = BuildWithInMemory("db1").GetRequiredService<TestDbContext>();
        var ctx2 = BuildWithInMemory("db2").GetRequiredService<TestDbContext>();

        ctx1.TestEntities.Add(TestEntity.Create("only-in-db1"));
        await ctx1.SaveChangesAsync();

        var countInDb2 = await ctx2.TestEntities.CountAsync();
        await Assert.That(countInDb2).IsEqualTo(0);
    }

    [Test]
    public async Task UseInMemoryDb_ParallelCalls_ProduceIsolatedDatabases()
    {
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var context = BuildWithInMemory($"parallel-db-{i}").GetRequiredService<TestDbContext>();
            context.TestEntities.Add(TestEntity.Create($"entity-{i}"));
            await context.SaveChangesAsync();
            return await context.TestEntities.CountAsync();
        }));

        var counts = await Task.WhenAll(tasks);
        await Assert.That(counts.All(c => c == 1)).IsTrue();
    }
}
