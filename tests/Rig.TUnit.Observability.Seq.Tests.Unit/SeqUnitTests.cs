using Rig.TUnit.Observability.Seq.Options;

namespace Rig.TUnit.Observability.Seq.Tests.Unit;

public sealed class SeqUnitTests
{
    [Test]
    public async Task SectionName_WithDefaultOptions_EqualsRigTUnitSeqPath()
    {
        // Arrange + Act
        var actual = SeqFixtureOptions.SectionName;

        // Assert
        await Assert.That(actual).IsEqualTo("RigTUnit:Seq");
    }

    [Test]
    public async Task Defaults_WithNoConfig_UseLatestImageAndDashboardCapture()
    {
        // Arrange + Act
        var options = new SeqFixtureOptions();

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("latest");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(60);
        await Assert.That(options.CaptureDashboardSnapshot).IsTrue();
        await Assert.That(options.SnapshotDirectory).IsEqualTo("TestResults/seq-dashboards");
    }

    [Test]
    public async Task SnapshotDirectory_WithExplicitOverride_TakesTheOverride()
    {
        // Arrange + Act
        var options = new SeqFixtureOptions { SnapshotDirectory = "custom/path" };

        // Assert
        await Assert.That(options.SnapshotDirectory).IsEqualTo("custom/path");
    }
}
