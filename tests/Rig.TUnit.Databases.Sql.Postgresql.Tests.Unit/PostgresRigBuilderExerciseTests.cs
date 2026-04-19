using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Postgresql.Builder;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit;

/// <summary>
/// T025a coverage-lifting test (FR-035): drives <see cref="PostgresRigBuilder"/>'s otherwise
/// under-exercised <c>UseProvider</c> override by calling the public <c>ReplaceDbContext</c>
/// flow, resolving the DbContext, and inspecting its options for the Npgsql extension.
/// No Docker required — the Builder path is pure wiring over <c>UseNpgsql</c>.
/// </summary>
public sealed class PostgresRigBuilderExerciseTests
{
    private const string SampleConnectionString = "Host=localhost;Database=test;Username=u;Password=p";

    [Test]
    public async Task ReplaceDbContext_WiresUpNpgsqlViaUseProvider_RegistersNpgsqlOptionsExtension()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new PostgresRigBuilder(captured!, source);

        // Act
        builder.ReplaceDbContext<SampleDbContext>();

        // Assert
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<SampleDbContext>>();

        var npgsqlExtension = options.Extensions
            .OfType<NpgsqlOptionsExtension>()
            .FirstOrDefault();

        await Assert.That(npgsqlExtension).IsNotNull();
        await Assert.That(npgsqlExtension!.ConnectionString).IsEqualTo(SampleConnectionString);
    }

    [Test]
    public async Task ReplaceDbContext_WithAdditionalConfig_InvokesBothPathsAndKeepsNpgsqlExtension()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new PostgresRigBuilder(captured!, source);
        var extraInvoked = 0;

        // Act
        builder.ReplaceDbContext<SampleDbContext>(opts => extraInvoked++);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<DbContextOptions<SampleDbContext>>();

        // Assert
        await Assert.That(extraInvoked).IsEqualTo(1);
    }

    [Test]
    public async Task ReplaceDbContextAdditionalConfig_NullAction_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var builder = new PostgresRigBuilder(captured!, RigConnect.FromValue(SampleConnectionString));

        // Act + Assert
        await Assert.That(() => builder.ReplaceDbContext<SampleDbContext>(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    private sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options);
}
