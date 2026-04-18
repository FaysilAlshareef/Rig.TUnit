using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Databases.NoSql.Dynamo.Options;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

public sealed class DynamoFixtureOptionsValidationTests
{
    [Test]
    public async Task DataAnnotations_DefaultOptions_PassValidation()
    {
        var options = new DynamoFixtureOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutOutOfRange_FailsValidation()
    {
        var options = new DynamoFixtureOptions { StartupTimeoutSeconds = 601 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(DynamoFixtureOptions.StartupTimeoutSeconds))))
            .IsTrue();
    }

    [Test]
    public async Task DataAnnotations_RegionEmpty_FailsValidation()
    {
        var options = new DynamoFixtureOptions { Region = string.Empty };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(DynamoFixtureOptions.Region))))
            .IsTrue();
    }

    [Test]
    public async Task DataAnnotations_ImageTagEmpty_FailsValidation()
    {
        var options = new DynamoFixtureOptions { ImageTag = string.Empty };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(DynamoFixtureOptions.ImageTag))))
            .IsTrue();
    }
}
