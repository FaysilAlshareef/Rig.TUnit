using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging.Detectors;
using Rig.TUnit.Observability.Logging.Fixtures;
using Rig.TUnit.Observability.Logging.Options;

namespace Rig.TUnit.Observability.Logging.Tests.Unit;

public sealed class AntiPatternDetectorTests
{
    private static LogEntry MakeEntry(
        string? originalFormat,
        IReadOnlyList<KeyValuePair<string, object?>>? properties = null) =>
        new(DateTimeOffset.UtcNow,
            LogLevel.Information,
            "TestCategory",
            default,
            "test message",
            originalFormat,
            properties ?? Array.Empty<KeyValuePair<string, object?>>(),
            Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>(),
            null);

    // ─── Detect — null guard ──────────────────────────────────────────────────

    [Test]
    public async Task Detect_NullEntries_ThrowsArgumentNullException()
    {
        // Arrange
        var detector = new AntiPatternDetector();

        // Act + Assert
        await Assert.That(() => detector.Detect(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    // ─── Detect — interpolated-template detection ────────────────────────────

    [Test]
    public async Task Detect_OriginalFormatWithNoPlaceholders_ReturnsInterpolatedViolation()
    {
        // Arrange — OriginalFormat has no `{` so LooksInterpolated = true
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = true,
            DetectPii = false,
        });
        var entry = MakeEntry(originalFormat: "Order processed successfully");

        // Act
        var violations = detector.Detect([entry]);

        // Assert
        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ViolationKind.InterpolatedTemplate);
    }

    [Test]
    public async Task Detect_OriginalFormatWithPlaceholders_ReturnsNoViolations()
    {
        // Arrange — OriginalFormat has `{OrderId}` so it is a structured template
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = true,
            DetectPii = false,
        });
        var entry = MakeEntry(originalFormat: "Order {OrderId} processed");

        // Act
        var violations = detector.Detect([entry]);

        // Assert
        await Assert.That(violations.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Detect_NullOriginalFormatWithNoProperties_ReturnsViolation()
    {
        // Arrange — OriginalFormat null + no properties → LooksInterpolated = true (no structured state)
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = true,
            DetectPii = false,
        });
        var entry = MakeEntry(originalFormat: null);

        // Act
        var violations = detector.Detect([entry]);

        // Assert
        await Assert.That(violations.Count).IsEqualTo(1);
    }

    // ─── Detect — PII property detection ─────────────────────────────────────

    [Test]
    public async Task Detect_PropertyNameMatchesCanonicalPiiToken_ReturnsPiiViolation()
    {
        // Arrange — "password" is a canonical PII token
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = false,
            DetectPii = true,
        });
        var props = new KeyValuePair<string, object?>[] { new("password", "s3cr3t") };
        var entry = MakeEntry(originalFormat: "User {password} logged in", properties: props);

        // Act
        var violations = detector.Detect([entry]);

        // Assert
        await Assert.That(violations.Count).IsEqualTo(1);
        await Assert.That(violations[0].Kind).IsEqualTo(ViolationKind.PiiPropertyName);
    }

    [Test]
    public async Task Detect_AdditionalPiiPattern_ReturnsViolationForMatchingProperty()
    {
        // Arrange — custom "ssn_\d+" pattern to catch "ssn_primary"
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = false,
            DetectPii = true,
            AdditionalPiiPatterns = ["custom_secret_\\w+"],
        });
        var props = new KeyValuePair<string, object?>[] { new("custom_secret_key", "val") };
        var entry = MakeEntry(originalFormat: "Event {custom_secret_key}", properties: props);

        // Act
        var violations = detector.Detect([entry]);

        // Assert
        await Assert.That(violations.Count).IsEqualTo(1);
    }

    // ─── AssertClean ─────────────────────────────────────────────────────────

    [Test]
    public async Task AssertClean_NullFixture_ThrowsArgumentNullException()
    {
        // Arrange
        var detector = new AntiPatternDetector();

        // Act + Assert
        await Assert.That(() => detector.AssertClean(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AssertClean_FixtureWithNoEntries_DoesNotThrow()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var detector = new AntiPatternDetector();

        // Act + Assert — empty fixture has no violations
        detector.AssertClean(fixture);
    }

    [Test]
    public async Task AssertClean_FixtureWithStructuredEntry_DoesNotThrow()
    {
        // Arrange — structured template with non-PII property
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<AntiPatternDetectorTests>();
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = true,
            DetectPii = true,
        });

        // Act
        logger.LogInformation("Order {OrderId} created", "ORD-001");

        // Assert — OrderId is not in the PII list and format has `{`
        detector.AssertClean(fixture);
    }

    [Test]
    public async Task AssertClean_FixtureWithPiiProperty_ThrowsAntiPatternDetectedException()
    {
        // Arrange — log entry with PII property "Password"
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<AntiPatternDetectorTests>();
        var detector = new AntiPatternDetector(new LoggingDetectorOptions
        {
            DetectInterpolatedTemplates = false,
            DetectPii = true,
        });

        // Act
        logger.LogInformation("User {Password} authenticated", "hunter2");

        // Assert — "Password" contains "password" → PII violation
        await Assert.That(() => detector.AssertClean(fixture))
            .ThrowsExactly<AntiPatternDetectedException>();
    }
}
