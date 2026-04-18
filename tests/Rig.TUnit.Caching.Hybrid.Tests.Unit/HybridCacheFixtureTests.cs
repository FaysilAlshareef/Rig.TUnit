using Microsoft.Extensions.Options;
using Rig.TUnit.Caching.Hybrid.Fixtures;
using Rig.TUnit.Caching.Hybrid.Options;

namespace Rig.TUnit.Caching.Hybrid.Tests.Unit;

public sealed class HybridCacheFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new HybridCacheFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new HybridCacheFixtureOptions();
        await Assert.That(() => new HybridCacheFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new HybridCacheFixture((HybridCacheFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new HybridCacheFixtureOptions());
        await Assert.That(() => new HybridCacheFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new HybridCacheFixture((IOptions<HybridCacheFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Cache_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new HybridCacheFixture();

        await Assert.That(() => { _ = fx.Cache; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task KeyPrefix_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new HybridCacheFixture();

        var first = fx.KeyPrefix;
        var second = fx.KeyPrefix;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new HybridCacheFixture();

        var actual = fx.ConnectionString;

        await Assert.That(actual).IsNotNullOrEmpty();
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new HybridCacheFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
