using System.Collections.Concurrent;

namespace Rig.TUnit.Parallelism.Helpers;

/// <summary>
/// Coordinates exclusive access to named resources. Tests that cannot run in
/// parallel on a shared resource (schema migration, global config, single-tenant
/// table) acquire a named lock. Per-name <see cref="SemaphoreSlim"/> under the hood.
/// </summary>
public sealed class ExclusiveResourceCoordinator
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    public static async Task<IDisposable> AcquireAsync(string resourceName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        var sem = Locks.GetOrAdd(resourceName, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new Releaser(sem);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _sem;
        private int _released;
        public Releaser(SemaphoreSlim sem) { _sem = sem; }
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0) _sem.Release();
        }
    }
}
