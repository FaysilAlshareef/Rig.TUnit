using System.ComponentModel.DataAnnotations;
using Rig.TUnit.Messaging.ServiceBus.Options;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 coverage-lifting validation tests for <see cref="ServiceBusFixtureOptions"/>.
/// Complements the construction + default tests by exercising the data-annotation
/// validators and the IValidatableObject.Validate branch for AcceptEula=false.
/// </summary>
public sealed class ServiceBusFixtureOptionsValidationTests
{
    [Test]
    public async Task DataAnnotations_DefaultOptions_PassValidation()
    {
        var options = new ServiceBusFixtureOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsTrue();
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutOutOfRange_FailsValidation()
    {
        var options = new ServiceBusFixtureOptions { StartupTimeoutSeconds = 601 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
        await Assert.That(results.Any(r => r.MemberNames.Contains(nameof(ServiceBusFixtureOptions.StartupTimeoutSeconds))))
            .IsTrue();
    }

    [Test]
    public async Task DataAnnotations_StartupTimeoutZero_FailsValidation()
    {
        var options = new ServiceBusFixtureOptions { StartupTimeoutSeconds = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        await Assert.That(valid).IsFalse();
    }

    [Test]
    public async Task Validate_AcceptEulaFalse_YieldsValidationResult()
    {
        var options = new ServiceBusFixtureOptions { AcceptEula = false };
        var context = new ValidationContext(options);

        var results = options.Validate(context).ToList();

        await Assert.That(results.Count).IsEqualTo(1);
        await Assert.That(results[0].MemberNames.Contains(nameof(ServiceBusFixtureOptions.AcceptEula))).IsTrue();
    }

    [Test]
    public async Task Validate_AcceptEulaTrue_YieldsNoValidationResults()
    {
        var options = new ServiceBusFixtureOptions { AcceptEula = true };
        var context = new ValidationContext(options);

        var results = options.Validate(context).ToList();

        await Assert.That(results).IsEmpty();
    }
}
