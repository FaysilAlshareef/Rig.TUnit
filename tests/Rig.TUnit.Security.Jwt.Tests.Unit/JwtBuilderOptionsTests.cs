using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Security.Jwt.Options;

namespace Rig.TUnit.Security.Jwt.Tests.Unit;

public sealed class JwtBuilderOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(JwtBuilderOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Jwt");
    }

    [Test]
    public async Task Options_Construct_WithRequiredProperties()
    {
        var opts = new JwtBuilderOptions
        {
            DefaultIssuer = "iss",
            DefaultAudience = "aud",
        };
        await Assert.That(opts.DefaultIssuer).IsEqualTo("iss");
        await Assert.That(opts.DefaultAudience).IsEqualTo("aud");
        await Assert.That(opts.DefaultLifetimeMinutes).IsEqualTo(60);
    }

    [Test]
    public async Task Options_OverrideLifetime_Takes()
    {
        var opts = new JwtBuilderOptions
        {
            DefaultIssuer = "iss",
            DefaultAudience = "aud",
            DefaultLifetimeMinutes = 5,
        };
        await Assert.That(opts.DefaultLifetimeMinutes).IsEqualTo(5);
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WhenRequiredPopulated()
    {
        var opts = new JwtBuilderOptions
        {
            DefaultIssuer = "iss",
            DefaultAudience = "aud",
        };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }
}
