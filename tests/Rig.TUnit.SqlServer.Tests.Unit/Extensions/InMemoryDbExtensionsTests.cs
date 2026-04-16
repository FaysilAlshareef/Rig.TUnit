using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.SqlServer.Extensions;
using Rig.TUnit.SqlServer.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.SqlServer.Tests.Unit.Extensions;

public class InMemoryDbExtensionsTests
{
    [Test]
    public async Task UseInMemoryDatabase_ReplacesExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseSqlServer("Server=fake;Database=fake;"));

        // Act
        services.UseInMemoryDatabase<TestDbContext>();

        // Assert
        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<TestDbContext>();
        await Assert.That(context.Database.ProviderName).IsEqualTo("Microsoft.EntityFrameworkCore.InMemory");
    }

    [Test]
    public async Task UseInMemoryDatabase_CalledTwice_UsesUniqueGuidNames()
    {
        // Arrange
        var services1 = new ServiceCollection();
        services1.UseInMemoryDatabase<TestDbContext>();
        var provider1 = services1.BuildServiceProvider();
        var context1 = provider1.GetRequiredService<TestDbContext>();

        var services2 = new ServiceCollection();
        services2.UseInMemoryDatabase<TestDbContext>();
        var provider2 = services2.BuildServiceProvider();
        var context2 = provider2.GetRequiredService<TestDbContext>();

        // Act — insert into context1, verify context2 is empty
        context1.TestEntities.Add(TestEntity.Create("only-in-db1"));
        await context1.SaveChangesAsync();

        var countInDb2 = await context2.TestEntities.CountAsync();

        // Assert — isolated databases mean context2 sees no data
        await Assert.That(countInDb2).IsEqualTo(0);
    }

    [Test]
    public async Task UseInMemoryDatabase_ParallelCalls_ProduceIsolatedDatabases()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            var services = new ServiceCollection();
            services.UseInMemoryDatabase<TestDbContext>();
            var provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<TestDbContext>();

            context.TestEntities.Add(TestEntity.Create($"entity-{i}"));
            await context.SaveChangesAsync();

            return await context.TestEntities.CountAsync();
        }));

        var counts = await Task.WhenAll(tasks);

        // Assert — each database should have exactly 1 entity (isolated)
        await Assert.That(counts.All(c => c == 1)).IsTrue();
    }
}
