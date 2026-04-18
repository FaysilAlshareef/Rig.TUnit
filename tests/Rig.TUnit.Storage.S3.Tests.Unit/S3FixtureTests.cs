using Microsoft.Extensions.Options;
using Rig.TUnit.Storage.S3.Fixtures;
using Rig.TUnit.Storage.S3.Options;

namespace Rig.TUnit.Storage.S3.Tests.Unit;

public sealed class S3FixtureTests
{
    [Test] public async Task Ctor_Parameterless_DoesNotThrow() => await Assert.That(() => new S3Fixture()).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptions_DoesNotThrow() => await Assert.That(() => new S3Fixture(new S3FixtureOptions())).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new S3Fixture((S3FixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    [Test] public async Task Ctor_WithIOptions_DoesNotThrow() => await Assert.That(() => new S3Fixture(Microsoft.Extensions.Options.Options.Create(new S3FixtureOptions()))).ThrowsNothing();
    [Test] public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new S3Fixture((IOptions<S3FixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new S3Fixture();
        await Assert.That(() => { _ = fx.ConnectionString; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new S3Fixture();
        await Assert.That(() => { _ = fx.Client; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ContainerName_BeforeInitialize_IsStableNonEmpty()
    {
        var fx = new S3Fixture();
        var a = fx.ContainerName;
        var b = fx.ContainerName;
        await Assert.That(a).IsNotNullOrEmpty();
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new S3Fixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
