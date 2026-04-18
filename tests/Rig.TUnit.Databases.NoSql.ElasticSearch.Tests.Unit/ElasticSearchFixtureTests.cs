using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Fixtures;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Options;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

public sealed class ElasticSearchFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new ElasticSearchFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new ElasticSearchFixtureOptions { ImageTag = "8.15.3" };
        await Assert.That(() => new ElasticSearchFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ElasticSearchFixture((ElasticSearchFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new ElasticSearchFixtureOptions());
        await Assert.That(() => new ElasticSearchFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new ElasticSearchFixture((IOptions<ElasticSearchFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new ElasticSearchFixture();
        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new ElasticSearchFixture();
        await Assert.That(() => { _ = fx.Client; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DatabaseName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new ElasticSearchFixture();
        var first = fx.DatabaseName;
        var second = fx.DatabaseName;
        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new ElasticSearchFixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
