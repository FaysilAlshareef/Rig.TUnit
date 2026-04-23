using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging.Assertions;
using Rig.TUnit.Observability.Logging.Fixtures;

namespace Rig.TUnit.Observability.Logging.Tests.Unit;

public sealed class LogAssertTests
{
    // ─── LogAssert.Logged ────────────────────────────────────────────────────

    [Test]
    public async Task Logged_NullFixture_ThrowsArgumentNullException()
    {
        await Assert.That(() => LogAssert.Logged(null!, LogLevel.Information))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Logged_FiltersByLevel_ReturnsSeparateEntriesPerLevel()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogInformation("Info message {Id}", 1);
        logger.LogWarning("Warning message {Id}", 2);

        // Assert
        var infoEntries = LogAssert.Logged(fixture, LogLevel.Information).Entries();
        var warnEntries = LogAssert.Logged(fixture, LogLevel.Warning).Entries();
        await Assert.That(infoEntries.Count).IsEqualTo(1);
        await Assert.That(warnEntries.Count).IsEqualTo(1);
    }

    // ─── LogEntryAssertion narrowing ──────────────────────────────────────────

    [Test]
    public async Task WithProperty_NarrowsToEntriesWithMatchingProperty()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogInformation("Order {OrderId} created", "order-42");
        logger.LogInformation("Order {OrderId} created", "order-99");

        // Assert
        var entries = LogAssert.Logged(fixture, LogLevel.Information)
            .WithProperty("OrderId", "order-42")
            .Entries();
        await Assert.That(entries.Count).IsEqualTo(1);
    }

    [Test]
    public async Task InScope_CalledOnFixture_NarrowsWithoutThrowing()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogInformation("No-scope message {Id}", 1);

        // Assert — no matching scope key, so narrows to empty; exercises InScope code path
        var entries = LogAssert.Logged(fixture, LogLevel.Information)
            .InScope("RequestId", "req-1")
            .Entries();
        await Assert.That(entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task WithMessage_NarrowsToEntriesContainingFragment()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogInformation("Hello {Action}", "world");
        logger.LogInformation("Goodbye {Action}", "world");

        // Assert
        var entries = LogAssert.Logged(fixture, LogLevel.Information)
            .WithMessage("Hello")
            .Entries();
        await Assert.That(entries.Count).IsEqualTo(1);
    }

    // ─── Once ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Once_ExactlyOneMatch_DoesNotThrow()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogError("Critical {Id}", 1);

        // Assert
        LogAssert.Logged(fixture, LogLevel.Error).Once();
    }

    [Test]
    public async Task Once_NoMatches_ThrowsLogAssertionException()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();

        // Act + Assert
        await Assert.That(() => LogAssert.Logged(fixture, LogLevel.Error).Once())
            .ThrowsExactly<LogAssertionException>();
    }

    [Test]
    public async Task Once_MultipleMatches_ThrowsLogAssertionException()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogError("Error A {Id}", 1);
        logger.LogError("Error B {Id}", 2);

        // Assert
        await Assert.That(() => LogAssert.Logged(fixture, LogLevel.Error).Once())
            .ThrowsExactly<LogAssertionException>();
    }

    // ─── Times ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Times_ExactMatchCount_DoesNotThrow()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogWarning("Warning {Id}", 1);
        logger.LogWarning("Warning {Id}", 2);

        // Assert
        LogAssert.Logged(fixture, LogLevel.Warning).Times(2);
    }

    [Test]
    public async Task Times_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();

        // Act + Assert
        await Assert.That(() => LogAssert.Logged(fixture, LogLevel.Information).Times(-1))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Times_CountMismatch_ThrowsLogAssertionException()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogDebug("Debug {Id}", 1);

        // Assert
        await Assert.That(() => LogAssert.Logged(fixture, LogLevel.Debug).Times(5))
            .ThrowsExactly<LogAssertionException>();
    }

    // ─── Entries ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Entries_ReturnsMaterialisedList()
    {
        // Arrange
        await using var fixture = new LoggingFixture();
        await fixture.InitializeAsync();
        var logger = fixture.CreateLogger<LogAssertTests>();

        // Act
        logger.LogInformation("Entry A {Id}", 1);
        logger.LogInformation("Entry B {Id}", 2);

        // Assert
        var entries = LogAssert.Logged(fixture, LogLevel.Information).Entries();
        await Assert.That(entries.Count).IsEqualTo(2);
    }
}
