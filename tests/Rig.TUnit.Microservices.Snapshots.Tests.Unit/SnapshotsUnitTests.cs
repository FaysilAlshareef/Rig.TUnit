using Rig.TUnit.Microservices.Snapshots.Scrubbers;

namespace Rig.TUnit.Microservices.Snapshots.Tests.Unit;

public sealed class SnapshotsUnitTests
{
    [Test]
    public async Task Apply_WithGuid_ReplacesWithPlaceholder()
    {
        var input = "{\"Id\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"}";

        var output = MicroserviceScrubbers.Apply(input);

        await Assert.That(output).Contains("{Guid}");
    }

    [Test]
    public async Task Apply_WithIsoTimestamp_ReplacesWithPlaceholder()
    {
        var input = "{\"OccurredAt\":\"2026-04-20T12:34:56Z\"}";

        var output = MicroserviceScrubbers.Apply(input);

        await Assert.That(output).Contains("{Timestamp}");
    }

    [Test]
    public async Task Apply_WithCorrelationId_ReplacesWithPlaceholder()
    {
        var input = "\"CorrelationId\":\"abc-123\"";

        var output = MicroserviceScrubbers.Apply(input);

        await Assert.That(output).Contains("{CorrelationLike}");
    }

    [Test]
    public async Task Apply_WithSequenceNumber_ReplacesWithPlaceholder()
    {
        var input = "\"Sequence\":42";

        var output = MicroserviceScrubbers.Apply(input);

        await Assert.That(output).Contains("{Sequence}");
    }

    [Test]
    public async Task Apply_WithNullInput_ThrowsArgumentNull()
    {
        await Assert.That(() => MicroserviceScrubbers.Apply(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Apply_WithConnectionString_ReplacesWithPlaceholder()
    {
        var input = "Server=localhost;Database=app;User Id=sa;Password=x;";

        var output = MicroserviceScrubbers.Apply(input);

        await Assert.That(output).Contains("{ConnectionString}");
    }
}
