using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Databases.Sql.Postgresql.Options;

namespace Rig.TUnit.Databases.Sql.Postgresql.Tests.Unit;

/// <summary>
/// T025a coverage-lifting validation tests for <see cref="PostgresFixtureOptions"/>.
/// Exercises defaults, override propagation through init-only autoprops, and the
/// data-annotation validators bound by <c>services.AddOptions&lt;T&gt;().ValidateDataAnnotations()</c>.
/// </summary>
public sealed class PostgresFixtureOptionsValidationTests
{
    [Test]
    public async Task SectionName_IsStableConstant()
    {
        // Bind through a local so the TUnit analyzer accepts the comparison.
        var actual = PostgresFixtureOptions.SectionName;
        await Assert.That(actual).IsEqualTo("RigTUnit:Postgres");
    }

    [Test]
    public async Task Defaults_WhenParameterless_MatchDocumentedValues()
    {
        // Act
        var options = new PostgresFixtureOptions();

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("16-alpine");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(120);
        await Assert.That(options.Username).IsEqualTo("postgres");
        await Assert.That(options.Password).IsEqualTo("postgres");
        await Assert.That(options.Database).IsEqualTo("rigtunit");
    }

    [Test]
    public async Task Overrides_EveryInitOnlyProperty_PropagatesValues()
    {
        // Act
        var options = new PostgresFixtureOptions
        {
            ImageTag = "15",
            StartupTimeoutSeconds = 60,
            Username = "alice",
            Password = "hunter2",
            Database = "orders",
        };

        // Assert
        await Assert.That(options.ImageTag).IsEqualTo("15");
        await Assert.That(options.StartupTimeoutSeconds).IsEqualTo(60);
        await Assert.That(options.Username).IsEqualTo("alice");
        await Assert.That(options.Password).IsEqualTo("hunter2");
        await Assert.That(options.Database).IsEqualTo("orders");
    }

    [Test]
    public async Task DataAnnotations_DefaultOptions_PassValidation()
    {
        // Arrange
        var options = new PostgresFixtureOptions();
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
        var options = new PostgresFixtureOptions { StartupTimeoutSeconds = 601 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // Assert
        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(PostgresFixtureOptions.StartupTimeoutSeconds))))
            .IsTrue();
    }
}
