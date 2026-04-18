using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Cassandra.Fixtures;
using Rig.TUnit.Databases.NoSql.Cassandra.Options;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit;

/// <summary>
/// T026-RED coverage-lifting tests for <see cref="CassandraFixture"/> — exercises every
/// constructor variant, null-guards, and pre-initialize-state exception paths without starting
/// a container. The container-bound <c>InitializeAsync</c> / <c>DisposeAsync</c> body is
/// covered by the integration suite; this file covers everything else.
/// </summary>
public sealed class CassandraFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new CassandraFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        // Arrange
        var options = new CassandraFixtureOptions { ImageTag = "5" };

        // Act + Assert
        await Assert.That(() => new CassandraFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new CassandraFixture((CassandraFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        // Arrange
        var wrapped = Microsoft.Extensions.Options.Options.Create(new CassandraFixtureOptions());

        // Act + Assert
        await Assert.That(() => new CassandraFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new CassandraFixture((IOptions<CassandraFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        // Arrange
        var fx = new CassandraFixture();

        // Act + Assert
        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Session_BeforeInitialize_ThrowsInvalidOperation()
    {
        // Arrange
        var fx = new CassandraFixture();

        // Act + Assert
        await Assert.That(() => { _ = fx.Session; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DatabaseName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        // Arrange
        var fx = new CassandraFixture();

        // Act
        var first = fx.DatabaseName;
        var second = fx.DatabaseName;

        // Assert
        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        // Arrange
        var fx = new CassandraFixture();

        // Act + Assert
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
