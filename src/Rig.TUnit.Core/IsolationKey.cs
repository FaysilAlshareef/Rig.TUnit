using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Rig.TUnit.Core;

/// <summary>
/// Deterministic per-test isolation token used as a suffix for Docker containers,
/// database names, topic names, and Redis key prefixes. Formula:
/// <c>{short-test-name:20}_{sha256(full-name):8}</c> — readable enough for log triage,
/// collision-resistant enough for parallel execution.
/// </summary>
/// <remarks>
/// Lives in <c>Rig.TUnit.Core</c> (not <c>Rig.TUnit.Databases</c>) so every base area
/// — Messaging, Caching, Storage, Observability, Security — can depend on it without
/// introducing a base-to-base reference that would violate the dependency-direction
/// architecture rule.
/// </remarks>
public readonly record struct IsolationKey(string Value)
{
    private const int ShortNameLength = 20;
    private const int HashLength = 8;

    public override string ToString() => Value;

    public static implicit operator string(IsolationKey key) => key.Value;

    /// <summary>
    /// Derives an isolation key from the currently-running test's fully-qualified name
    /// (or a supplied identifier for scenarios where the test context is unavailable).
    /// </summary>
    public static IsolationKey FromExecutionContext(string? fullyQualifiedName = null)
    {
        var source = string.IsNullOrWhiteSpace(fullyQualifiedName)
            ? Environment.GetEnvironmentVariable("RIGTUNIT_TEST_ID") ?? Guid.NewGuid().ToString("N")
            : fullyQualifiedName;
        return FromName(source);
    }

    /// <summary>Derives an isolation key from any stable identifier.</summary>
    public static IsolationKey FromName(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        var normalized = Normalize(fullName);
        var shortName = normalized.Length <= ShortNameLength
            ? normalized
            : normalized[^ShortNameLength..];

        var hash = ComputeHash(fullName);
        return new IsolationKey($"{shortName}_{hash}");
    }

    /// <summary>
    /// Docker container names: lowercase, 1-63 chars, <c>[a-z0-9][a-z0-9_.-]*</c>.
    /// Truncates to 63 chars and lowercases the payload.
    /// </summary>
    public string ForDockerContainer()
    {
        var lower = Value.ToLowerInvariant();
        return lower.Length <= 63 ? lower : lower[..63];
    }

    /// <summary>Postgres database names: 63-byte cap, lowercase identifiers conventional.</summary>
    public string ForPostgresDatabase()
    {
        var lower = Value.ToLowerInvariant();
        return lower.Length <= 63 ? lower : lower[..63];
    }

    /// <summary>SQL Server database names: 128-char cap; keep original casing.</summary>
    public string ForSqlServerDatabase()
    {
        return Value.Length <= 128 ? Value : Value[..128];
    }

    /// <summary>Redis key prefix: free-form, but we trim to 64 chars to keep keys readable.</summary>
    public string ForRedisKeyPrefix()
    {
        return Value.Length <= 64 ? Value : Value[..64];
    }

    private static string Normalize(string source)
    {
        var sb = new StringBuilder(source.Length);
        foreach (var ch in source)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (ch is '.' or '_' or '-')
            {
                sb.Append('_');
            }
        }
        return sb.Length > 0 ? sb.ToString() : "test";
    }

    private static string ComputeHash(string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var digest = SHA256.HashData(bytes);
        var sb = new StringBuilder(HashLength);
        for (var i = 0; i < HashLength / 2; i++)
        {
            sb.Append(digest[i].ToString("x2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
