using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Databases.NoSql.Mongo.Options;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Unit;

/// <summary>
/// T025a coverage-lifting validation tests for <see cref="MongoFixtureOptions"/>.
/// Complements the construction + default tests by exercising the data-annotation
/// validators bound through <c>services.AddOptions&lt;T&gt;().ValidateDataAnnotations()</c>.
/// </summary>
public sealed class MongoFixtureOptionsValidationTests
{
    [Test]
    public async Task DataAnnotations_DefaultOptions_PassValidation()
    {
        // Arrange
        var options = new MongoFixtureOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsTrue();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutOutOfRange_FailsValidation()
    {
        // Arrange — [Range(1,600)] on StartupTimeoutSeconds
        var options = new MongoFixtureOptions { StartupTimeoutSeconds = 601 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(MongoFixtureOptions.StartupTimeoutSeconds))))
            .IsTrue();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutZero_FailsValidation()
    {
        // Arrange
        var options = new MongoFixtureOptions { StartupTimeoutSeconds = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsFalse();
    }
}
