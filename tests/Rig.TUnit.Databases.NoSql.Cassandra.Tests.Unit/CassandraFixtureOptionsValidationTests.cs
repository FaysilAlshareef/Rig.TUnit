using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Databases.NoSql.Cassandra.Options;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit;

/// <summary>
/// T026-RED coverage-lifting validation tests for <see cref="CassandraFixtureOptions"/>.
/// Exercises every data-annotation branch that <c>services.AddOptions&lt;T&gt;().ValidateDataAnnotations()</c>
/// can trigger.
/// </summary>
public sealed class CassandraFixtureOptionsValidationTests
{
    [Test]
    public async Task DataAnnotations_DefaultOptions_PassValidation()
    {
        // Arrange
        var options = new CassandraFixtureOptions();
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
        var options = new CassandraFixtureOptions { StartupTimeoutSeconds = 601 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(CassandraFixtureOptions.StartupTimeoutSeconds))))
            .IsTrue();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutZero_FailsValidation()
    {
        // Arrange
        var options = new CassandraFixtureOptions { StartupTimeoutSeconds = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task DataAnnotations_ImageTagEmpty_FailsValidation()
    {
        // Arrange — [Required] on ImageTag
        var options = new CassandraFixtureOptions { ImageTag = string.Empty };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(CassandraFixtureOptions.ImageTag))))
            .IsTrue();
    }
}
