using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Options;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Tests.Unit;

public sealed class ElasticSearchFixtureOptionsValidationTests
{
    [Test]
    public async Task DataAnnotations_DefaultOptions_PassValidation()
    {
        var options = new ElasticSearchFixtureOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutOutOfRange_FailsValidation()
    {
        var options = new ElasticSearchFixtureOptions { StartupTimeoutSeconds = 601 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task DataAnnotations_ImageTagEmpty_FailsValidation()
    {
        var options = new ElasticSearchFixtureOptions { ImageTag = string.Empty };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
