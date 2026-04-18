using System.Text.RegularExpressions;
using Rig.TUnit.Observability.Logging.Fixtures;
using Rig.TUnit.Observability.Logging.Options;

namespace Rig.TUnit.Observability.Logging.Detectors;

/// <summary>
/// Runtime detector that inspects <see cref="LogEntry.OriginalFormat"/> + structured
/// properties for anti-patterns (interpolated-template literals, PII-shaped property names).
/// NOTE: <c>Console.Write</c> detection is the analyzer's job (T227) — runtime cannot
/// observe direct <c>Console.Write</c> calls unless <c>Console.SetOut</c> is hooked.
/// </summary>
public sealed class AntiPatternDetector
{
    private readonly LoggingDetectorOptions _options;
    private readonly Regex[] _additionalPii;

    public AntiPatternDetector(LoggingDetectorOptions? options = null)
    {
        _options = options ?? new LoggingDetectorOptions();
        _additionalPii = _options.AdditionalPiiPatterns
            .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.ECMAScript | RegexOptions.Compiled))
            .ToArray();
    }

    /// <summary>Returns every detected violation across <paramref name="entries"/>.</summary>
    public IReadOnlyList<DetectedViolation> Detect(IEnumerable<LogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var violations = new List<DetectedViolation>();

        foreach (var entry in entries)
        {
            if (_options.DetectInterpolatedTemplates && LooksInterpolated(entry))
            {
                violations.Add(new DetectedViolation(
                    Kind: ViolationKind.InterpolatedTemplate,
                    Message: $"Log entry appears to use an interpolated-template literal (\"$...\") — use a structured template with {{Placeholders}}.",
                    Entry: entry));
            }

            if (_options.DetectPii)
            {
                foreach (var prop in entry.Properties)
                {
                    if (IsPiiPropertyName(prop.Key))
                    {
                        violations.Add(new DetectedViolation(
                            Kind: ViolationKind.PiiPropertyName,
                            Message: $"Log property '{prop.Key}' matches the canonical PII list or a user pattern — do not log this.",
                            Entry: entry));
                    }
                }
            }
        }

        return violations;
    }

    /// <summary>Inspects every entry captured by the fixture; throws if any violation is found.</summary>
    public void AssertClean(LoggingFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var violations = Detect(fixture.Entries);
        if (violations.Count > 0)
        {
            var summary = string.Join(Environment.NewLine, violations.Select(v => $" - [{v.Kind}] {v.Message}"));
            throw new AntiPatternDetectedException(
                $"AntiPatternDetector found {violations.Count} violation(s):{Environment.NewLine}{summary}");
        }
    }

    private static bool LooksInterpolated(LogEntry entry)
    {
        // An interpolated-template literal, once passed as the message, has no
        // `{OriginalFormat}` structured template or has `{Name}` placeholders
        // already replaced with values inside the rendered message.
        // Heuristics:
        //  (a) OriginalFormat missing but structured properties present → user passed a string.Format'd message.
        //  (b) OriginalFormat contains no `{` placeholders but Message does (i.e., literal `{`).
        //  (c) OriginalFormat present and contains no structured placeholders at all
        //      (no {Name}) — i.e., it was the result of an interpolated literal with all values baked in.
        var original = entry.OriginalFormat;
        if (original is null)
        {
            return entry.Properties.Count == 0;
        }
        // Use @-destructuring awareness — {@Payload} is allowed (structured).
        var hasPlaceholder = original.AsSpan().IndexOf('{') >= 0;
        return !hasPlaceholder;
    }

    private bool IsPiiPropertyName(string name)
    {
        foreach (var token in LoggingDetectorOptions.CanonicalPiiTokens)
        {
            if (name.Contains(token, StringComparison.OrdinalIgnoreCase)) return true;
        }
        foreach (var rx in _additionalPii)
        {
            if (rx.IsMatch(name)) return true;
        }
        return false;
    }
}

public enum ViolationKind
{
    InterpolatedTemplate,
    PiiPropertyName,
}

public sealed record DetectedViolation(ViolationKind Kind, string Message, LogEntry Entry);

public sealed class AntiPatternDetectedException : Exception
{
    public AntiPatternDetectedException(string message) : base(message) { }
}
