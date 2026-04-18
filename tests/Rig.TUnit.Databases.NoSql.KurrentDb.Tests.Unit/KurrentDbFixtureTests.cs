using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.KurrentDb.Fixtures;
using Rig.TUnit.Databases.NoSql.KurrentDb.Options;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

public sealed class KurrentDbFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new KurrentDbFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new KurrentDbFixtureOptions { ImageTag = "25.1" };
        await Assert.That(() => new KurrentDbFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new KurrentDbFixture((KurrentDbFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new KurrentDbFixtureOptions());
        await Assert.That(() => new KurrentDbFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new KurrentDbFixture((IOptions<KurrentDbFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new KurrentDbFixture();
        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new KurrentDbFixture();
        await Assert.That(() => { _ = fx.Client; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DatabaseName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new KurrentDbFixture();
        var first = fx.DatabaseName;
        var second = fx.DatabaseName;
        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new KurrentDbFixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
