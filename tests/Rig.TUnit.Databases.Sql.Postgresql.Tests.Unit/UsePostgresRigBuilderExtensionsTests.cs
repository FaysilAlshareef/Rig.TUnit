using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Builder;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit;

/// <summary>
/// Retroactive unit coverage for <see cref="PostgresRigBuilderExtensions.UsePostgres"/> —
/// backfilled under T176a (the Phase 3.0 commit 2b149b2 shipped the src without a RED).
/// </summary>
public sealed class UsePostgresRigBuilderExtensionsTests
{
    [Test]
    public async Task UsePostgres_NullRig_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue("Host=localhost;Database=test;Username=u;Password=p");

        await Assert.That(() =>
                ((RigBuilder)null!).UsePostgres(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_NullSource_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);

        await Assert.That(() =>
                captured!.UsePostgres(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Host=localhost;Database=test;Username=u;Password=p");

        await Assert.That(() =>
                captured!.UsePostgres(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePostgres_WithValidArgs_ReturnsSameRigBuilderForFluentChain()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Host=localhost;Database=test;Username=u;Password=p");

        var returned = captured!.UsePostgres(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UsePostgres_WithValidArgs_InvokesConfigureExactlyOnce()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Host=localhost;Database=test;Username=u;Password=p");
        var invocations = 0;

        captured!.UsePostgres(source, _ => invocations++);

        await Assert.That(invocations).IsEqualTo(1);
    }

    [Test]
    public async Task UsePostgres_ConfigureReceivesPostgresRigBuilderInstance()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Host=localhost;Database=test;Username=u;Password=p");
        PostgresRigBuilder? configured = null;

        captured!.UsePostgres(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
    }
}
