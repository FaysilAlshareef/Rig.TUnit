using Microsoft.Extensions.Caching.Memory;
using Rig.TUnit.Caching.Fixtures;

namespace Rig.TUnit.Caching.Memory.Fixtures;

/// <summary>
/// In-process <see cref="IMemoryCache"/>-backed cache fixture. No container —
/// coherency / backplane tests N/A (single-node by definition).
/// </summary>
public sealed class MemoryCacheFixture : CacheFixtureBase
{
    private MemoryCache? _cache;
    private bool _initialized;
    private bool _disposed;

    public IMemoryCache Cache => _cache
        ?? throw new InvalidOperationException("MemoryCacheFixture not initialised — call InitializeAsync first.");

    public override string ConnectionString => "in-memory";

    public override Task InitializeAsync()
    {
        if (_initialized) return Task.CompletedTask;
        _cache = new MemoryCache(new MemoryCacheOptions());
        _initialized = true;
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _cache?.Dispose();
        _cache = null;
        return ValueTask.CompletedTask;
    }
}
