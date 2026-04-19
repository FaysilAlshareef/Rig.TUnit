using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Observability.AppInsights.Options;

namespace Rig.TUnit.Observability.AppInsights.Tests.Unit;

public sealed class AppInsightsFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(AppInsightsFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:AppInsights");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new AppInsightsFixtureOptions();
        await Assert.That(opts.InstrumentationKey).IsNotNullOrEmpty();
        await Assert.That(opts.RoleName).IsEqualTo("rigtunit-tests");
    }

    [Test]
    public async Task Options_OverrideRoleName_Takes()
    {
        var opts = new AppInsightsFixtureOptions { RoleName = "orders-service" };
        await Assert.That(opts.RoleName).IsEqualTo("orders-service");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new AppInsightsFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }
}
