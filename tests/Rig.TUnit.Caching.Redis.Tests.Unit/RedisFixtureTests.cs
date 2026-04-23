using Microsoft.Extensions.Options;
using Rig.TUnit.Caching.Redis.Fixtures;
using Rig.TUnit.Caching.Redis.Options;

namespace Rig.TUnit.Caching.Redis.Tests.Unit;

/// <summary>
/// Unit coverage for <see cref="RedisFixture"/> constructors and guards.
/// Live container lifecycle is exercised by Integration tests only.
/// </summary>
public sealed class RedisFixtureTests
{
    [Test]
    public async Task DefaultCtor_DoesNotThrow()
    {
        var fixture = new RedisFixture();

        await Assert.That(fixture).IsNotNull();
    }

    [Test]
    public async Task DirectOptionsCtor_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() => new RedisFixture((RedisFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task IOptionsCtor_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.That(() => new RedisFixture((IOptions<RedisFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task IOptionsCtor_ValidOptions_DoesNotThrow()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new RedisFixtureOptions { ImageTag = "7-alpine" });
        var fixture = new RedisFixture(opts);

        await Assert.That(fixture).IsNotNull();
    }

    [Test]
    public async Task ConnectionString_BeforeInitializeAsync_ThrowsInvalidOperationException()
    {
        var fixture = new RedisFixture();

        await Assert.That(() => fixture.ConnectionString).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DisposeAsync_WithoutPriorInit_DoesNotThrow()
    {
        var fixture = new RedisFixture();

        // should complete without throwing — no container to dispose
        await fixture.DisposeAsync();
    }
}
