using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Databases.Sql.MySql.Options;

namespace Rig.TUnit.Databases.Sql.MySql.Tests.Unit;

public sealed class MySqlFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(MySqlFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:MySql");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new MySqlFixtureOptions();
        await Assert.That(opts.ImageTag).IsEqualTo("8.4");
        await Assert.That(opts.StartupTimeoutSeconds).IsEqualTo(180);
        await Assert.That(opts.Username).IsEqualTo("root");
        await Assert.That(opts.Password).IsEqualTo("rigtunit");
        await Assert.That(opts.Database).IsEqualTo("rigtunit");
    }

    [Test]
    public async Task Options_OverrideImageTag_Takes()
    {
        var opts = new MySqlFixtureOptions { ImageTag = "8.0" };
        await Assert.That(opts.ImageTag).IsEqualTo("8.0");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new MySqlFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task Options_InvalidTimeout_Fails()
    {
        var opts = new MySqlFixtureOptions { StartupTimeoutSeconds = 0 };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
