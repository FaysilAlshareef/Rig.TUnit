using System.Text.RegularExpressions;
using Cassandra;
using Rig.TUnit.Core;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Helpers;

/// <summary>
/// Isolates tests by keyspace — each test gets a fresh keyspace name derived from
/// <see cref="IsolationKey"/>, and the keyspace is dropped on dispose. Cheaper than
/// recreating the Cassandra container per test.
/// </summary>
/// <remarks>
/// <see cref="BuildSafeKeyspace"/> is the security-critical path: CQL DDL statements
/// are string-concatenated from the returned identifier, so every untrusted input
/// has to go through the <see cref="SafePrefix"/> whitelist before it reaches Cassandra.
/// </remarks>
public sealed class KeyspacePerTestHelper : IAsyncDisposable
{
    private static readonly Regex SafePrefix = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);
    private const int MaxKeyspaceLength = 48;

    private readonly ISession _session;
    private bool _disposed;

    private KeyspacePerTestHelper(ISession session, string keyspaceName)
    {
        _session = session;
        KeyspaceName = keyspaceName;
    }

    public string KeyspaceName { get; }

    /// <summary>
    /// Builds a Cassandra-safe keyspace name by combining <paramref name="prefix"/> with
    /// <paramref name="isolation"/>. Validates <paramref name="prefix"/> against
    /// <c>^[a-z][a-z0-9_]*$</c> (CQL identifier rules), sanitises the isolation segment to
    /// lowercase alphanumeric, and caps the total length at 48 chars (Cassandra's CQL limit).
    /// </summary>
    public static string BuildSafeKeyspace(string prefix, IsolationKey isolation)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            throw new ArgumentException("prefix is required.", nameof(prefix));
        }
        if (!SafePrefix.IsMatch(prefix))
        {
            throw new ArgumentException(
                "prefix must match ^[a-z][a-z0-9_]*$ (Cassandra CQL identifier rules).",
                nameof(prefix));
        }
        if (string.IsNullOrEmpty(isolation.Value))
        {
            throw new ArgumentException("IsolationKey.Value is null or empty.", nameof(isolation));
        }

        var sanitised = new string(isolation.Value
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray());

        var candidate = $"{prefix}_{sanitised}";
        if (candidate.Length > MaxKeyspaceLength)
        {
            candidate = candidate[..MaxKeyspaceLength];
        }
        return candidate;
    }

    public static async Task<KeyspacePerTestHelper> CreateAsync(
        ISession session,
        IsolationKey isolation,
        string prefix = "test")
    {
        ArgumentNullException.ThrowIfNull(session);
        var name = BuildSafeKeyspace(prefix, isolation);
        await session.ExecuteAsync(new SimpleStatement(
            $"CREATE KEYSPACE IF NOT EXISTS {name} WITH replication = {{'class':'SimpleStrategy','replication_factor':1}}"))
            .ConfigureAwait(false);
        return new KeyspacePerTestHelper(session, name);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _session.ExecuteAsync(new SimpleStatement($"DROP KEYSPACE IF EXISTS {KeyspaceName}"))
            .ConfigureAwait(false);
    }
}
