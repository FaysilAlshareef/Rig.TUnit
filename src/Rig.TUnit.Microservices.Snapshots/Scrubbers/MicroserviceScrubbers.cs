using System.Text.RegularExpressions;

namespace Rig.TUnit.Microservices.Snapshots.Scrubbers;

/// <summary>
/// Microservice-opinionated scrubbers. Applies in order — each replaces matches
/// with a deterministic placeholder so snapshots are stable across runs.
/// Covers: correlation/causation IDs, event IDs, timestamps, sequence numbers,
/// connection strings, filesystem paths.
/// </summary>
public static class MicroserviceScrubbers
{
    private static readonly (Regex Pattern, string Replacement)[] Rules =
    {
        (new Regex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled),
         "{Guid}"),
        (new Regex(@"\b\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?\b", RegexOptions.Compiled),
         "{Timestamp}"),
        (new Regex(@"""(CorrelationId|CausationId)""\s*:\s*""[^""]+""", RegexOptions.Compiled),
         @"""$1"":""{CorrelationLike}"""),
        (new Regex(@"""(EventId|MessageId)""\s*:\s*""[^""]+""", RegexOptions.Compiled),
         @"""$1"":""{EventLike}"""),
        (new Regex(@"""(Sequence|SequenceNumber)""\s*:\s*\d+", RegexOptions.Compiled),
         @"""$1"":{Sequence}"),
        (new Regex(@"Server=[^;""]+;Database=[^;""]+;User Id=[^;""]+;Password=[^;""]+;?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
         "{ConnectionString}"),
        (new Regex(@"""(ConnectionString)""\s*:\s*""[^""]+""", RegexOptions.Compiled),
         @"""$1"":""{ConnectionString}"""),
        (new Regex(@"[A-Za-z]:\\[^""\s]+", RegexOptions.Compiled),
         "{WindowsPath}"),
        (new Regex(@"/(home|Users|tmp|var)/[^""\s]+", RegexOptions.Compiled),
         "{UnixPath}"),
    };

    public static string Apply(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var result = input;
        foreach (var (pattern, replacement) in Rules)
        {
            result = pattern.Replace(result, replacement);
        }
        return result;
    }
}
