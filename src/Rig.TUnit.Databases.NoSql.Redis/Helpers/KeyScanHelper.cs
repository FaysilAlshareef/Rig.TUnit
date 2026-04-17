using StackExchange.Redis;

namespace Rig.TUnit.Databases.NoSql.Redis.Helpers;

/// <summary>
/// <c>SCAN</c>-based key enumeration. Tests MUST use this instead of <c>KEYS</c> to
/// avoid blocking the Redis server thread.
/// </summary>
public sealed class KeyScanHelper
{
    private readonly IDatabase _db;

    public KeyScanHelper(IConnectionMultiplexer connection, int db = -1)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _db = connection.GetDatabase(db);
    }

    public async IAsyncEnumerable<string> ScanAsync(
        string pattern,
        int pageSize = 100,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints()[0]);
        await foreach (var key in server.KeysAsync(_db.Database, pattern, pageSize).WithCancellation(ct))
        {
            yield return key.ToString();
        }
    }
}
