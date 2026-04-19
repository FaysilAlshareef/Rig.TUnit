using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Security.Mtls.Options;

namespace Rig.TUnit.Security.Mtls.Tests.Unit;

public sealed class MtlsFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(MtlsFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Mtls");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new MtlsFixtureOptions();
        await Assert.That(opts.CaSubject).IsEqualTo("CN=rigtunit-test-ca");
        await Assert.That(opts.ClientSubject).IsEqualTo("CN=rigtunit-client");
        await Assert.That(opts.ServerSubject).IsEqualTo("CN=rigtunit-server");
        await Assert.That(opts.ValidityDays).IsEqualTo(365);
    }

    [Test]
    public async Task Options_OverrideValidity_Takes()
    {
        var opts = new MtlsFixtureOptions { ValidityDays = 30 };
        await Assert.That(opts.ValidityDays).IsEqualTo(30);
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new MtlsFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }
}
