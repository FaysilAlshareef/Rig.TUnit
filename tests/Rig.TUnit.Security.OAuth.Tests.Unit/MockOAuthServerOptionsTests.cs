using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Security.OAuth.Options;

namespace Rig.TUnit.Security.OAuth.Tests.Unit;

public sealed class MockOAuthServerOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(MockOAuthServerOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:OAuth");
    }

    [Test]
    public async Task Options_Construct_WithRequiredProperties()
    {
        var opts = new MockOAuthServerOptions { Issuer = "https://host/" };
        await Assert.That(opts.Issuer).IsEqualTo("https://host/");
        await Assert.That(opts.Port).IsEqualTo(0);
        await Assert.That(opts.TokenLifetimeSeconds).IsEqualTo(3600);
        await Assert.That(opts.SupportedFlows.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Options_OverridePort_Takes()
    {
        var opts = new MockOAuthServerOptions { Issuer = "https://host/", Port = 5055 };
        await Assert.That(opts.Port).IsEqualTo(5055);
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WhenRequiredPopulated()
    {
        var opts = new MockOAuthServerOptions { Issuer = "https://host/" };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }
}
