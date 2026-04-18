using Microsoft.Extensions.Options;
using Rig.TUnit.Storage.MinIO.Fixtures;
using Rig.TUnit.Storage.MinIO.Options;

namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

public sealed class MinIOFixtureTests
{
    [Test] public async Task Ctor_Parameterless_DoesNotThrow() => await Assert.That(() => new MinIOFixture()).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptions_DoesNotThrow() => await Assert.That(() => new MinIOFixture(new MinIOFixtureOptions())).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new MinIOFixture((MinIOFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    [Test] public async Task Ctor_WithIOptions_DoesNotThrow() => await Assert.That(() => new MinIOFixture(Microsoft.Extensions.Options.Options.Create(new MinIOFixtureOptions()))).ThrowsNothing();
    [Test] public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new MinIOFixture((IOptions<MinIOFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new MinIOFixture();
        await Assert.That(() => { _ = fx.ConnectionString; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new MinIOFixture();
        await Assert.That(() => { _ = fx.Client; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ContainerName_BeforeInitialize_IsStableNonEmpty()
    {
        var fx = new MinIOFixture();
        var a = fx.ContainerName;
        var b = fx.ContainerName;
        await Assert.That(a).IsNotNullOrEmpty();
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new MinIOFixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
