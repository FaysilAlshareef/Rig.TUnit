using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Security.Policies.Options;

namespace Rig.TUnit.Security.Policies.Tests.Unit;

public sealed class PolicyFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(PolicyFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Policies");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new PolicyFixtureOptions();
        await Assert.That(opts.DefaultScheme).IsEqualTo("Test");
        await Assert.That(opts.RequiredClaims.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Options_OverrideScheme_Takes()
    {
        var opts = new PolicyFixtureOptions { DefaultScheme = "Bearer" };
        await Assert.That(opts.DefaultScheme).IsEqualTo("Bearer");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new PolicyFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }
}
