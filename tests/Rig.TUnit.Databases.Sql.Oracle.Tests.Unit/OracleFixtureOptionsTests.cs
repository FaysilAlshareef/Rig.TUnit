using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Databases.Sql.Oracle.Options;

namespace Rig.TUnit.Databases.Sql.Oracle.Tests.Unit;

public sealed class OracleFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(OracleFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Oracle");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new OracleFixtureOptions();
        await Assert.That(opts.Image).Contains("gvenzl/oracle-free");
        await Assert.That(opts.StartupTimeoutSeconds).IsEqualTo(300);
        await Assert.That(opts.Username).IsEqualTo("rigtunit");
        await Assert.That(opts.Password).IsEqualTo("rigtunit");
    }

    [Test]
    public async Task Options_OverrideImage_Takes()
    {
        var opts = new OracleFixtureOptions { Image = "gvenzl/oracle-free:23.4" };
        await Assert.That(opts.Image).IsEqualTo("gvenzl/oracle-free:23.4");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new OracleFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task Options_InvalidTimeout_Fails()
    {
        var opts = new OracleFixtureOptions { StartupTimeoutSeconds = 30 };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
