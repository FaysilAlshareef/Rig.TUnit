using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Rig.TUnit.Docker.Options;

namespace Rig.TUnit.Docker.Tests.Unit;

public sealed class DockerFixtureOptionsTests
{
    [Test]
    public async Task SectionName_Field_ExistsAsPublicConstString()
    {
        var field = typeof(DockerFixtureOptions).GetField(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        await Assert.That(field).IsNotNull();
        await Assert.That(field!.IsLiteral).IsTrue();
        await Assert.That(field.FieldType).IsEqualTo(typeof(string));
        await Assert.That((string?)field.GetRawConstantValue()).IsEqualTo("RigTUnit:Docker");
    }

    [Test]
    public async Task Options_Construct_WithDefaults()
    {
        var opts = new DockerFixtureOptions();
        await Assert.That(opts.DefaultImage).IsEqualTo("alpine:3");
        await Assert.That(opts.IsolatePerTestNetwork).IsTrue();
        await Assert.That(opts.ReuseImageCache).IsTrue();
        await Assert.That(opts.DefaultStartupTimeoutSeconds).IsEqualTo(300);
    }

    [Test]
    public async Task Options_OverrideImage_Takes()
    {
        var opts = new DockerFixtureOptions { DefaultImage = "debian:stable-slim" };
        await Assert.That(opts.DefaultImage).IsEqualTo("debian:stable-slim");
    }

    [Test]
    public async Task Options_ValidateDataAnnotations_Passes_WithDefaults()
    {
        var opts = new DockerFixtureOptions();
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsTrue();
    }

    [Test]
    public async Task Options_InvalidTimeout_Fails()
    {
        var opts = new DockerFixtureOptions { DefaultStartupTimeoutSeconds = 1 };
        var ctx = new ValidationContext(opts);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(opts, ctx, results, validateAllProperties: true);
        await Assert.That(valid).IsFalse();
    }
}
