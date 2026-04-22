using NSubstitute;
using Rig.TUnit.Caching.Assertions;

namespace Rig.TUnit.Caching.Tests.Unit;

public sealed class CacheAssertTests
{
    [Test]
    public async Task Stampede_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.Stampede(null!, "key", 5))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Stampede_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.StampedeAsync("key", 5, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.Stampede(probe, "key", 5);

        await probe.Received(1).StampedeAsync("key", 5, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TagInvalidation_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.TagInvalidation(null!, "tag"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task TagInvalidation_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.TagInvalidationAsync("tag", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.TagInvalidation(probe, "tag");

        await probe.Received(1).TagInvalidationAsync("tag", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Coherent_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.Coherent(null!, "key"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task FailSafe_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.FailSafe(null!, "key"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task NegativeCached_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.NegativeCached(null!, "key"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HitRate_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.HitRate(null!, 0.9))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EagerRefresh_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => CacheAssert.EagerRefresh(null!, "key"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EagerRefresh_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.EagerRefreshAsync("key", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.EagerRefresh(probe, "key");

        await probe.Received(1).EagerRefreshAsync("key", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Coherent_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.CoherentAsync("key", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.Coherent(probe, "key");

        await probe.Received(1).CoherentAsync("key", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FailSafe_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.FailSafeAsync("key", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.FailSafe(probe, "key");

        await probe.Received(1).FailSafeAsync("key", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NegativeCached_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.NegativeCachedAsync("key", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.NegativeCached(probe, "key");

        await probe.Received(1).NegativeCachedAsync("key", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HitRate_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<ICacheProbe>();
        probe.HitRateAsync(0.95, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await CacheAssert.HitRate(probe, 0.95);

        await probe.Received(1).HitRateAsync(0.95, Arg.Any<CancellationToken>());
    }
}
