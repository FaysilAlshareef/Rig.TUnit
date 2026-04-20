using Rig.TUnit.Observability.Tracing.Options;

namespace Rig.TUnit.Observability.Tracing.Tests.Unit;

public sealed class TracingUnitTests
{
    [Test]
    public async Task SectionName_WithDefaultOptions_EqualsRigTUnitTracingPath()
    {
        // Arrange + Act
        var actual = TracingFixtureOptions.SectionName;

        // Assert
        await Assert.That(actual).IsEqualTo("RigTUnit:Tracing");
    }

    [Test]
    public async Task Defaults_WithRequiredServiceName_UseFullSamplingAndTenKSpanBuffer()
    {
        // Arrange + Act
        var options = new TracingFixtureOptions { ServiceName = "svc" };

        // Assert
        await Assert.That(options.ServiceName).IsEqualTo("svc");
        await Assert.That(options.SampleRatio).IsEqualTo(1.0);
        await Assert.That(options.MaxSpansInMemory).IsEqualTo(10000);
    }

    [Test]
    public async Task SampleRatio_WithCustomValue_TakesTheOverride()
    {
        // Arrange + Act
        var options = new TracingFixtureOptions { ServiceName = "svc", SampleRatio = 0.25 };

        // Assert
        await Assert.That(options.SampleRatio).IsEqualTo(0.25);
    }
}
