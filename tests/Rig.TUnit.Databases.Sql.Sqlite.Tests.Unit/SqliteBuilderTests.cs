using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;
using Rig.TUnit.Databases.Sql.Sqlite.Builder;

namespace Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit;

public sealed class SqliteBuilderTests
{
    private const string InMemoryCs = "Data Source=:memory:";

    [Test]
    public async Task SqliteRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(SqliteRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task SqliteRigBuilder_TypeMetadata_InheritsSqlRigBuilderCrtp()
    {
        var baseType = typeof(SqliteRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType.GetGenericTypeDefinition()).IsEqualTo(typeof(SqlRigBuilder<>));
        await Assert.That(baseType.GenericTypeArguments[0]).IsEqualTo(typeof(SqliteRigBuilder));
    }

    [Test]
    public async Task SqliteRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue(InMemoryCs);

        await Assert.That(() => new SqliteRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SqliteRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new SqliteRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlite_NullRig_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue(InMemoryCs);

        await Assert.That(() => ((RigBuilder)null!).UseSqlite(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlite_NullSource_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseSqlite(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlite_NullConfigure_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(InMemoryCs);

        await Assert.That(() => captured!.UseSqlite(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseSqlite_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(InMemoryCs);

        var returned = captured!.UseSqlite(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseSqlite_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(InMemoryCs);
        var calls = 0;

        captured!.UseSqlite(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseSqlite_ValidArgs_PassesSqliteRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(InMemoryCs);
        SqliteRigBuilder? passed = null;

        captured!.UseSqlite(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }

    [Test]
    public async Task SqliteRigBuilder_ReplaceDbContext_RegistersContextWithSqlite()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(InMemoryCs);

        var builder = new SqliteRigBuilder(captured!, source);
        builder.ReplaceDbContext<SampleDbContext>();

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();

        await Assert.That(context).IsNotNull();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
