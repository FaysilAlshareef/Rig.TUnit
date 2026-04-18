using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Observability.Metrics.Options;

namespace Rig.TUnit.Observability.Metrics.Tests.Unit;

public sealed class MetricsFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(MetricsFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Metrics");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new MetricsFixtureOptions();
        await Assert.That(opts.MeterName).IsEqualTo("Rig.TUnit.Metrics");
        await Assert.That(opts.MaxTagCardinality).IsEqualTo(100);
    }

    [Test]
    public async Task Options_OverrideMeterName_Takes()
    {
        var opts = new MetricsFixtureOptions { MeterName = "orders.service" };
        await Assert.That(opts.MeterName).IsEqualTo("orders.service");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new MetricsFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task Options_InvalidCardinality_Fails()
    {
        var opts = new MetricsFixtureOptions { MaxTagCardinality = 0 };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
