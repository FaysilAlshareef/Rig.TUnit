using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.Sql.Postgresql.Fixtures;
using Rig.TUnit.Databases.Sql.Postgresql.Options;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit;

/// <summary>
/// T025a coverage-lifting tests for <see cref="PostgresFixture"/> — exercises constructor
/// variants, null-guards, and pre-initialize-state exception paths without starting a
/// container. The container-bound <c>InitializeAsync</c> / <c>DisposeAsync</c> body is
/// covered by the integration suite; this file covers everything else.
/// </summary>
public sealed class PostgresFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        // Act + Assert
        await Assert.That(() => new PostgresFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_Parameterless_DatabaseName_ReturnsNonEmptyIsolatedValue()
    {
        // Arrange
        await using var fx = new PostgresFixture();

        // Act
        var name = fx.DatabaseName;

        // Assert
        await Assert.That(name).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        // Arrange
        var options = new PostgresFixtureOptions { ImageTag = "15-alpine" };

        // Act + Assert
        await Assert.That(() => new PostgresFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        // Act + Assert
        await Assert.That(() => new PostgresFixture((PostgresFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        // Arrange
        var wrapped = Microsoft.Extensions.Options.Options.Create(new PostgresFixtureOptions());

        // Act + Assert
        await Assert.That(() => new PostgresFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        // Act + Assert
        await Assert.That(() => new PostgresFixture((IOptions<PostgresFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        // Arrange
        await using var fx = new PostgresFixture();

        // Act + Assert
        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        // Arrange
        var fx = new PostgresFixture();

        // Act + Assert
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }

    [Test]
    public async Task DatabaseName_CalledTwice_IsStableForSameInstance()
    {
        // Arrange
        await using var fx = new PostgresFixture();

        // Act
        var first = fx.DatabaseName;
        var second = fx.DatabaseName;

        // Assert
        await Assert.That(first).IsEqualTo(second);
    }
}
