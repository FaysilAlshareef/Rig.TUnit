using System.Xml.Linq;

namespace Rig.TUnit.Ci.Enrichers;

/// <summary>
/// Enriches a TRX (Visual Studio test-result XML) file with extra context —
/// span IDs, container logs, screenshot paths — surfaced by CI dashboards.
/// </summary>
public sealed class TrxEnricher
{
    public int EnrichFile(string trxPath, IReadOnlyDictionary<string, IReadOnlyList<string>> enrichments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trxPath);
        ArgumentNullException.ThrowIfNull(enrichments);

        var doc = XDocument.Load(trxPath);
        XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var enriched = 0;
        foreach (var result in doc.Descendants(ns + "UnitTestResult"))
        {
            var testName = result.Attribute("testName")?.Value ?? string.Empty;
            if (!enrichments.TryGetValue(testName, out var items) || items.Count == 0) continue;

            var output = result.Element(ns + "Output") ?? new XElement(ns + "Output");
            if (output.Parent is null) result.Add(output);

            var stdout = output.Element(ns + "StdOut") ?? new XElement(ns + "StdOut");
            if (stdout.Parent is null) output.Add(stdout);

            foreach (var item in items)
            {
                stdout.Add(new XText($"{Environment.NewLine}[enricher] {item}"));
            }
            enriched++;
        }
        doc.Save(trxPath);
        return enriched;
    }
}

/// <summary>Tracks flaky tests — those that toggle pass/fail across runs.</summary>
public sealed class FlakyQuarantine
{
    private readonly Dictionary<string, int> _failures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _successes = new(StringComparer.Ordinal);

    public void RecordFailure(string testName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);
        _failures[testName] = _failures.GetValueOrDefault(testName) + 1;
    }

    public void RecordSuccess(string testName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);
        _successes[testName] = _successes.GetValueOrDefault(testName) + 1;
    }

    public IReadOnlyList<string> Flaky()
    {
        var keys = _failures.Keys.Concat(_successes.Keys).Distinct();
        return keys.Where(k => _failures.GetValueOrDefault(k) > 0 && _successes.GetValueOrDefault(k) > 0).ToArray();
    }
}

/// <summary>Guards against coverage regression.</summary>
public sealed record CoverageDeltaEnforcer(double Minimum)
{
    public bool IsAcceptable(double current, double baseline) => current >= baseline - Minimum;
}
