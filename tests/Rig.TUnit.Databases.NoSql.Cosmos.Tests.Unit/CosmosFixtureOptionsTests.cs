using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Databases.NoSql.Cosmos.Options;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Tests.Unit;

public sealed class CosmosFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(CosmosFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Cosmos");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new CosmosFixtureOptions();
        await Assert.That(opts.Image).Contains("vnext-preview");
        await Assert.That(opts.DatabaseName).IsEqualTo("rigtunit");
        await Assert.That(opts.StartupTimeoutSeconds).IsEqualTo(300);
    }

    [Test]
    public async Task Options_OverrideDatabaseName_Takes()
    {
        var opts = new CosmosFixtureOptions { DatabaseName = "orders" };
        await Assert.That(opts.DatabaseName).IsEqualTo("orders");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new CosmosFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }
}
