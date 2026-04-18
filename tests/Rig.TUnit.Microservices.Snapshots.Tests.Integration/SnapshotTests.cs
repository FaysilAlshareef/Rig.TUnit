using Rig.TUnit.Microservices.Snapshots.Assertions;
using Rig.TUnit.Microservices.Snapshots.Scrubbers;

namespace Rig.TUnit.Microservices.Snapshots.Tests.Integration;

public sealed class SnapshotTests
{
    private static string NewDir() => Path.Combine(Path.GetTempPath(), $"rigtunit-snap-{Guid.NewGuid():N}");

    [Test]
    public async Task Match_FirstRun_CreatesVerifiedFile()
    {
        // Arrange
        var dir = NewDir();

        // Act
        var result = await SnapshotAssert.Match("hello", "snap1", dir);

        // Assert
        await Assert.That(result.Outcome).IsEqualTo(SnapshotOutcome.FirstRun);
        await Assert.That(File.Exists(result.VerifiedPath)).IsTrue();
    }

    [Test]
    public async Task Match_SecondRunWithSameContent_ReturnsMatched()
    {
        // Arrange
        var dir = NewDir();
        await SnapshotAssert.Match("hello", "snap2", dir);

        // Act
        var result = await SnapshotAssert.Match("hello", "snap2", dir);

        // Assert
        await Assert.That(result.Outcome).IsEqualTo(SnapshotOutcome.Matched);
    }

    [Test]
    public async Task Match_SecondRunWithMismatch_ThrowsWithDiff()
    {
        // Arrange
        var dir = NewDir();
        await SnapshotAssert.Match("line1\nline2", "snap3", dir);

        // Act
        async Task Action() => await SnapshotAssert.Match("line1\nCHANGED", "snap3", dir);

        // Assert
        await Assert.ThrowsAsync<SnapshotAssertionException>(Action);
    }

    [Test]
    public async Task Scrubbers_ReplaceGuidTimestampConnectionStringAndPaths()
    {
        // Arrange
        var raw = """
        {
          "Id": "550e8400-e29b-41d4-a716-446655440000",
          "CorrelationId": "abc-123",
          "OccurredAt": "2026-04-17T18:22:00.123Z",
          "Sequence": 42,
          "ConnectionString": "Server=prod;Database=orders;User Id=sa;Password=secret;"
        }
        """;

        // Act
        var scrubbed = MicroserviceScrubbers.Apply(raw);

        // Assert
        await Assert.That(scrubbed).Contains("{Guid}");
        await Assert.That(scrubbed).Contains("{Timestamp}");
        await Assert.That(scrubbed).Contains("{CorrelationLike}");
        await Assert.That(scrubbed).Contains("{Sequence}");
        await Assert.That(scrubbed).Contains("{ConnectionString}");
    }

    [Test]
    public async Task MatchJson_IndentsAndApplieScrubbers()
    {
        // Arrange
        var dir = NewDir();
        var payload = new { Id = Guid.NewGuid(), Name = "x" };

        // Act
        var result = await SnapshotAssert.MatchJson(payload, "snap4", dir);
        var verified = await File.ReadAllTextAsync(result.VerifiedPath);

        // Assert
        await Assert.That(verified).Contains("{Guid}");
        await Assert.That(verified).Contains("\"Name\"");
    }

    [Test]
    public async Task Scrubbers_NonMatchingText_UnchangedRoundTrip()
    {
        // Arrange
        var raw = "just plain text";

        // Act
        var scrubbed = MicroserviceScrubbers.Apply(raw);

        // Assert
        await Assert.That(scrubbed).IsEqualTo(raw);
    }
}
