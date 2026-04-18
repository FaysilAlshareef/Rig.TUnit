using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging.Detectors;
using Rig.TUnit.Observability.Logging.Fixtures;
using Rig.TUnit.Observability.Logging.Options;

namespace Rig.TUnit.Observability.Logging.Tests.Integration;

public sealed class AntiPatternDetectorTests
{
    private static async Task<LoggingFixture> CreateAsync()
    {
        var fx = new LoggingFixture();
        await fx.InitializeAsync();
        return fx;
    }

    [Test]
    public async Task Detector_FlagsInterpolatedLiteral()
    {
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");
        var id = 99;
        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
#pragma warning disable CA2254
        log.LogInformation($"Processing order {id}");
#pragma warning restore CA2254

        var violations = new AntiPatternDetector().Detect(fx.Entries);
        await Assert.That(violations.Count).IsGreaterThan(0);
        await Assert.That(violations.Any(v => v.Kind == ViolationKind.InterpolatedTemplate)).IsTrue();
    }

    [Test]
    public async Task Detector_AllowsStructuredTemplate()
    {
        await using var fx = await CreateAsync();
        fx.CreateLogger("T").LogInformation("Processing order {OrderId}", 42);

        var violations = new AntiPatternDetector().Detect(fx.Entries);
        await Assert.That(violations.Any(v => v.Kind == ViolationKind.InterpolatedTemplate)).IsFalse();
    }

    [Test]
    public async Task Detector_CanonicalPiiList_FlagsEveryToken()
    {
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");

        foreach (var token in LoggingDetectorOptions.CanonicalPiiTokens)
        {
            // Log a property whose name exactly matches a canonical PII token.
            log.Log(LogLevel.Information, new EventId(1),
                new[] { new KeyValuePair<string, object?>(token, "redacted") },
                null, (_, _) => $"entry for {token}");
        }

        var violations = new AntiPatternDetector().Detect(fx.Entries);
        var piiViolations = violations.Where(v => v.Kind == ViolationKind.PiiPropertyName).ToArray();
        await Assert.That(piiViolations.Length).IsGreaterThanOrEqualTo(LoggingDetectorOptions.CanonicalPiiTokens.Length);
    }

    [Test]
    public async Task Detector_AdditionalPiiPatterns_AreRespected()
    {
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");
        log.Log(LogLevel.Information, new EventId(1),
            new[] { new KeyValuePair<string, object?>("internalIdNumber", "x") },
            null, (_, _) => "e");

        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            AdditionalPiiPatterns = [@"internal.*id"],
        });
        var violations = detector.Detect(fx.Entries);
        await Assert.That(violations.Any(v => v.Kind == ViolationKind.PiiPropertyName)).IsTrue();
    }

    [Test]
    public async Task Detector_CanonicalTokens_CannotBeDisabled()
    {
        // Even with DetectPii=true and a custom AdditionalPiiPatterns list,
        // the canonical tokens are always effective — they are hard-coded.
        await using var fx = await CreateAsync();
        fx.CreateLogger("T").Log(LogLevel.Information, new EventId(1),
            new[] { new KeyValuePair<string, object?>("password", "p") },
            null, (_, _) => "e");

        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectPii = true,
            AdditionalPiiPatterns = Array.Empty<string>(),
        });
        await Assert.That(detector.Detect(fx.Entries).Any(v => v.Kind == ViolationKind.PiiPropertyName)).IsTrue();
    }

    [Test]
    public async Task Detector_AssertClean_ThrowsOnViolation()
    {
        await using var fx = await CreateAsync();
        fx.CreateLogger("T").Log(LogLevel.Information, new EventId(1),
            new[] { new KeyValuePair<string, object?>("Password", "x") },
            null, (_, _) => "e");

        var threw = false;
        try { new AntiPatternDetector().AssertClean(fx); }
        catch (AntiPatternDetectedException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Detector_AssertClean_PassesWhenClean()
    {
        await using var fx = await CreateAsync();
        fx.CreateLogger("T").LogInformation("Processing order {OrderId}", 42);

        new AntiPatternDetector().AssertClean(fx);
    }
}
