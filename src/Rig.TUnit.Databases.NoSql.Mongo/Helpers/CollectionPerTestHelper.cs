using MongoDB.Driver;
using Rig.TUnit.Core;

namespace Rig.TUnit.Databases.NoSql.Mongo.Helpers;

/// <summary>
/// Isolates tests by collection — each test gets a fresh collection name derived from
/// <see cref="IsolationKey"/>, and the collection is dropped on dispose. Cheaper than
/// spinning a fresh database per test when the test does not need schema isolation.
/// </summary>
public sealed class CollectionPerTestHelper : IAsyncDisposable
{
    private readonly IMongoDatabase _database;
    private readonly string _collectionName;
    private bool _disposed;

    public CollectionPerTestHelper(IMongoDatabase database, IsolationKey isolation, string prefix = "test")
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
        _collectionName = $"{prefix}_{isolation.Value}";
    }

    public string CollectionName => _collectionName;

    public IMongoCollection<T> GetCollection<T>() => _database.GetCollection<T>(_collectionName);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        await _database.DropCollectionAsync(_collectionName).ConfigureAwait(false);
    }
}
