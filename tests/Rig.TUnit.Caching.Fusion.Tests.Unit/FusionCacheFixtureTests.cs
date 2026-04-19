using Microsoft.Extensions.Options;
using Rig.TUnit.Caching.Fusion.Fixtures;
using Rig.TUnit.Caching.Fusion.Options;

namespace Rig.TUnit.Caching.Fusion.Tests.Unit;

public sealed class FusionCacheFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new FusionCacheFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new FusionCacheFixtureOptions();
        await Assert.That(() => new FusionCacheFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new FusionCacheFixture((FusionCacheFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new FusionCacheFixtureOptions());
        await Assert.That(() => new FusionCacheFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new FusionCacheFixture((IOptions<FusionCacheFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Cache_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new FusionCacheFixture();

        await Assert.That(() => { _ = fx.Cache; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task KeyPrefix_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new FusionCacheFixture();

        var first = fx.KeyPrefix;
        var second = fx.KeyPrefix;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new FusionCacheFixture();

        var actual = fx.ConnectionString;

        await Assert.That(actual).IsNotNullOrEmpty();
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new FusionCacheFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
