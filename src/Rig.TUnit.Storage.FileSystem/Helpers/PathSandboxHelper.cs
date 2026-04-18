namespace Rig.TUnit.Storage.FileSystem.Helpers;

/// <summary>
/// Pure-function sandbox-path resolver. Enforces that the resolved absolute path
/// stays inside the configured <c>root</c> — blocks <c>../</c> traversals and
/// absolute paths pointing outside the sandbox root.
/// </summary>
public static class PathSandboxHelper
{
    public static string Resolve(string root, string relative)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relative);

        var rootFull = Path.GetFullPath(root);
        var candidate = Path.IsPathRooted(relative)
            ? Path.GetFullPath(relative)
            : Path.GetFullPath(Path.Combine(rootFull, relative));

        if (!IsInside(rootFull, candidate))
        {
            throw new UnauthorizedAccessException(
                $"Path '{relative}' resolves outside sandbox root '{rootFull}'.");
        }

        return candidate;
    }

    public static bool IsInside(string root, string candidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate);

        var rootFull = Path.GetFullPath(root);
        var candidateFull = Path.IsPathRooted(candidate)
            ? Path.GetFullPath(candidate)
            : Path.GetFullPath(Path.Combine(rootFull, candidate));

        // Normalize both to the same separator / trailing slash.
        var normalizedRoot = rootFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;
        var normalizedCandidate = candidateFull.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return (normalizedCandidate + Path.DirectorySeparatorChar)
            .StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }
}
