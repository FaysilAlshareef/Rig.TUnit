using Microsoft.EntityFrameworkCore;

namespace Rig.TUnit.SqlServer.Tests.Unit.TestInfrastructure;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();
}
