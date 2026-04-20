using Microsoft.Extensions.Caching.Memory;
using Rig.TUnit.Caching.Memory.Fixtures;

namespace Rig.TUnit.Caching.Memory.Tests.Unit;

/// <summary>
/// Pure unit coverage for <see cref="MemoryCacheFixture"/>.
/// </summary>
public sealed class MemoryCacheUnitTests
{
    [Test]
    public async Task ConnectionString_BeforeInitialize_IsInMemory()
    {
        await using var fixture = new MemoryCacheFixture();

        await Assert.That(fixture.ConnectionString).IsEqualTo("in-memory");
    }

    [Test]
    public async Task Cache_BeforeInitializeAsync_ThrowsInvalidOperation()
    {
        await using var fixture = new MemoryCacheFixture();

        await Assert.That(() => fixture.Cache).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Cache_AfterInitializeAsync_ExposesUsableInstance()
    {
        await using var fixture = new MemoryCacheFixture();
        await fixture.InitializeAsync();

        fixture.Cache.Set("key", "value");
        var found = fixture.Cache.TryGetValue<string>("key", out var value);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo("value");
    }

    [Test]
    public async Task InitializeAsync_CalledTwice_IsIdempotent()
    {
        await using var fixture = new MemoryCacheFixture();

        await fixture.InitializeAsync();
        var first = fixture.Cache;
        await fixture.InitializeAsync();
        var second = fixture.Cache;

        await Assert.That(first).IsSameReferenceAs(second);
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var fixture = new MemoryCacheFixture();
        await fixture.InitializeAsync();

        await fixture.DisposeAsync();
        // Second dispose is idempotent — fixture tracks the _disposed flag.
        await fixture.DisposeAsync();

        await Assert.That(fixture.ConnectionString).IsEqualTo("in-memory");
    }
}
