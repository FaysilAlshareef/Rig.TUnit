using System.Text.Json;
using Rig.TUnit.Microservices.Snapshots.Scrubbers;

namespace Rig.TUnit.Microservices.Snapshots.Assertions;

/// <summary>
/// Snapshot-testing assertion compatible with the Verify.TUnit file-naming convention
/// (<c>{name}.received.*</c> / <c>{name}.verified.*</c>). First-run writes a
/// <c>.received</c> file, copies it to <c>.verified</c> if there is no verified
/// baseline, and asserts equal on subsequent runs. Microservice-opinionated
/// scrubbers are applied before comparison.
/// </summary>
public static class SnapshotAssert
{
    /// <summary>
    /// Matches <paramref name="actual"/> against the snapshot stored at
    /// <paramref name="directory"/>/<paramref name="name"/>.verified.<paramref name="extension"/>.
    /// </summary>
    public static async Task<SnapshotResult> Match(
        string actual,
        string name,
        string directory,
        string extension = "txt",
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        Directory.CreateDirectory(directory);
        var scrubbed = MicroserviceScrubbers.Apply(actual);

        var received = Path.Combine(directory, $"{name}.received.{extension}");
        var verified = Path.Combine(directory, $"{name}.verified.{extension}");

        await File.WriteAllTextAsync(received, scrubbed, ct);

        if (!File.Exists(verified))
        {
            // First run — promote received to verified, caller must review & commit.
            File.Copy(received, verified, overwrite: true);
            return SnapshotResult.FirstRun(verified);
        }

        var expected = await File.ReadAllTextAsync(verified, ct);
        if (string.Equals(expected, scrubbed, StringComparison.Ordinal))
        {
            // Match — clean up the received artifact so CI doesn't upload a duplicate.
            if (File.Exists(received)) File.Delete(received);
            return SnapshotResult.Match(verified);
        }

        throw new SnapshotAssertionException(
            name, verified, received,
            BuildDiff(expected, scrubbed));
    }

    /// <summary>
    /// Convenience overload for object snapshots — serialises to indented JSON,
    /// then scrubs + matches.
    /// </summary>
    public static Task<SnapshotResult> MatchJson<T>(
        T actual,
        string name,
        string directory,
        JsonSerializerOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(actual, options);
        return Match(json, name, directory, extension: "json", ct);
    }

    private static string BuildDiff(string expected, string actual)
    {
        // Minimal line-level diff — readable, no external tooling.
        var e = expected.Split('\n');
        var a = actual.Split('\n');
        var diff = new System.Text.StringBuilder();
        var max = Math.Max(e.Length, a.Length);
        for (var i = 0; i < max; i++)
        {
            var el = i < e.Length ? e[i] : "<missing>";
            var al = i < a.Length ? a[i] : "<missing>";
            if (!string.Equals(el, al, StringComparison.Ordinal))
            {
                diff.AppendLine($"  line {i + 1,3}: - {el.TrimEnd('\r')}");
                diff.AppendLine($"  line {i + 1,3}: + {al.TrimEnd('\r')}");
            }
        }
        return diff.ToString();
    }
}

public sealed record SnapshotResult(SnapshotOutcome Outcome, string VerifiedPath)
{
    public static SnapshotResult FirstRun(string verified) => new(SnapshotOutcome.FirstRun, verified);
    public static SnapshotResult Match(string verified) => new(SnapshotOutcome.Matched, verified);
}

public enum SnapshotOutcome { FirstRun, Matched }

public sealed class SnapshotAssertionException : Exception
{
    public string Name { get; }
    public string VerifiedPath { get; }
    public string ReceivedPath { get; }
    public string Diff { get; }

    public SnapshotAssertionException(string name, string verifiedPath, string receivedPath, string diff)
        : base($"Snapshot mismatch for '{name}'.\n  verified: {verifiedPath}\n  received: {receivedPath}\n\nDiff:\n{diff}")
    {
        Name = name;
        VerifiedPath = verifiedPath;
        ReceivedPath = receivedPath;
        Diff = diff;
    }
}
