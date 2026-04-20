using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging;
using Rig.TUnit.Observability.Logging.Options;

namespace Rig.TUnit.Observability.Logging.Tests.Unit;

/// <summary>
/// Pure unit coverage for <see cref="LoggingFixtureOptions"/> + <see cref="LogEntry"/> record.
/// </summary>
public sealed class LoggingUnitTests
{
    [Test]
    public async Task SectionName_WithDefaultOptions_EqualsRigTUnitLoggingPath()
    {
        // Arrange + Act
        var actual = LoggingFixtureOptions.SectionName;

        // Assert
        await Assert.That(actual).IsEqualTo("RigTUnit:Logging");
    }

    [Test]
    public async Task Defaults_WithNoConfig_UseDebugLevelAndFiftyThousandEntries()
    {
        // Arrange + Act
        var options = new LoggingFixtureOptions();

        // Assert
        await Assert.That(options.MinLevel).IsEqualTo(LogLevel.Debug);
        await Assert.That(options.MaxEntriesInMemory).IsEqualTo(50_000);
    }

    [Test]
    public async Task LogEntry_WithSameFieldValues_IsValueEqual()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var first = new LogEntry(now, LogLevel.Information, "Cat", new EventId(1), "m", null,
            Array.Empty<KeyValuePair<string, object?>>(),
            Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>(), null);
        var second = first with { };

        // Assert
        await Assert.That(first).IsEqualTo(second);
    }
}
