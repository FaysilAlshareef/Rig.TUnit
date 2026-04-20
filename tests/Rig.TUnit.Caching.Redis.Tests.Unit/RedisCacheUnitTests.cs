using Microsoft.Extensions.Options;
using NSubstitute;
using Rig.TUnit.Caching.Redis.Helpers;
using Rig.TUnit.Caching.Redis.Options;
using StackExchange.Redis;

namespace Rig.TUnit.Caching.Redis.Tests.Unit;

/// <summary>
/// Pure unit coverage for <see cref="RedisFixtureOptions"/> + <see cref="RedisBackplaneCapture"/>
/// constructor guards. Live pub/sub behaviour is in Integration.
/// </summary>
public sealed class RedisCacheUnitTests
{
    [Test]
    public async Task RedisFixtureOptions_SectionName_IsStable()
    {
        var actual = RedisFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Redis");
    }

    [Test]
    public async Task RedisFixtureOptions_Defaults_AreSane()
    {
        var options = new RedisFixtureOptions();

        await Assert.That(options.ImageTag).IsEqualTo("7-alpine");
        await Assert.That(options.Database).IsEqualTo(0);
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(60);
    }

    [Test]
    public async Task RedisFixtureOptions_InitExplicitly_OverridesDefaults()
    {
        var options = new RedisFixtureOptions
        {
            ImageTag = "7.2-bookworm",
            Database = 3,
            StartupTimeoutSeconds = 120,
        };

        await Assert.That(options.ImageTag).IsEqualTo("7.2-bookworm");
        await Assert.That(options.Database).IsEqualTo(3);
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
    }

    [Test]
    public async Task RedisBackplaneCapture_Ctor_RejectsNullConnection()
    {
        await Assert.That(() => new RedisBackplaneCapture(null!, "ch"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RedisBackplaneCapture_Ctor_RejectsEmptyChannel()
    {
        var mux = Substitute.For<IConnectionMultiplexer>();
        await Assert.That(() => new RedisBackplaneCapture(mux, ""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task RedisFixtureOptions_CanBeWrappedInIOptions()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RedisFixtureOptions { ImageTag = "custom" });

        await Assert.That(options.Value.ImageTag).IsEqualTo("custom");
    }
}
