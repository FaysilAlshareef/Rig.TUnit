using Microsoft.Extensions.Options;
using Rig.TUnit.Storage.AzureBlob.Fixtures;
using Rig.TUnit.Storage.AzureBlob.Options;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

public sealed class AzureBlobFixtureTests
{
    [Test] public async Task Ctor_Parameterless_DoesNotThrow() => await Assert.That(() => new AzureBlobFixture()).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptions_DoesNotThrow() => await Assert.That(() => new AzureBlobFixture(new AzureBlobFixtureOptions())).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new AzureBlobFixture((AzureBlobFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    [Test] public async Task Ctor_WithIOptions_DoesNotThrow() => await Assert.That(() => new AzureBlobFixture(Microsoft.Extensions.Options.Options.Create(new AzureBlobFixtureOptions()))).ThrowsNothing();
    [Test] public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new AzureBlobFixture((IOptions<AzureBlobFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new AzureBlobFixture();
        await Assert.That(() => { _ = fx.ConnectionString; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new AzureBlobFixture();
        await Assert.That(() => { _ = fx.Client; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ContainerName_BeforeInitialize_IsStableNonEmpty()
    {
        var fx = new AzureBlobFixture();
        var a = fx.ContainerName;
        var b = fx.ContainerName;
        await Assert.That(a).IsNotNullOrEmpty();
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new AzureBlobFixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
