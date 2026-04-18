using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Mongo.Fixtures;
using Rig.TUnit.Databases.NoSql.Mongo.Options;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit;

/// <summary>
/// T025a coverage-lifting tests for <see cref="MongoFixture"/> — exercises every
/// constructor variant, null-guards, and pre-initialize-state exception paths without
/// starting a container. The container-bound <c>InitializeAsync</c> / <c>DisposeAsync</c>
/// body is covered by the integration suite; this file covers everything else.
/// </summary>
public sealed class MongoFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        // Act + Assert
        await Assert.That(() => new MongoFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        // Arrange
        var options = new MongoFixtureOptions { ImageTag = "7" };

        // Act + Assert
        await Assert.That(() => new MongoFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        // Act + Assert
        await Assert.That(() => new MongoFixture((MongoFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        // Arrange
        var wrapped = Microsoft.Extensions.Options.Options.Create(new MongoFixtureOptions());

        // Act + Assert
        await Assert.That(() => new MongoFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        // Act + Assert
        await Assert.That(() => new MongoFixture((IOptions<MongoFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        // Arrange
        var fx = new MongoFixture();

        // Act + Assert
        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Database_BeforeInitialize_ThrowsInvalidOperation()
    {
        // Arrange
        var fx = new MongoFixture();

        // Act + Assert
        await Assert.That(() => { _ = fx.Database; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DatabaseName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        // Arrange
        var fx = new MongoFixture();

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
        var fx = new MongoFixture();

        // Act + Assert
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
