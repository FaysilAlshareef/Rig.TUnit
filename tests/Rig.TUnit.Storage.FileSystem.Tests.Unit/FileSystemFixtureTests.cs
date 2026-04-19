using Microsoft.Extensions.Options;
using Rig.TUnit.Storage.FileSystem.Fixtures;
using Rig.TUnit.Storage.FileSystem.Options;

namespace Rig.TUnit.Storage.FileSystem.Tests.Unit;

public sealed class FileSystemFixtureTests
{
    [Test] public async Task Ctor_Parameterless_DoesNotThrow() => await Assert.That(() => new FileSystemFixture()).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptions_DoesNotThrow() => await Assert.That(() => new FileSystemFixture(new FileSystemFixtureOptions())).ThrowsNothing();
    [Test] public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new FileSystemFixture((FileSystemFixtureOptions)null!)).ThrowsExactly<ArgumentNullException>();
    [Test] public async Task Ctor_WithIOptions_DoesNotThrow() => await Assert.That(() => new FileSystemFixture(Microsoft.Extensions.Options.Options.Create(new FileSystemFixtureOptions()))).ThrowsNothing();
    [Test] public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException() => await Assert.That(() => new FileSystemFixture((IOptions<FileSystemFixtureOptions>)null!)).ThrowsExactly<ArgumentNullException>();

    [Test]
    public async Task Root_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new FileSystemFixture();
        await Assert.That(() => { _ = fx.Root; }).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task ContainerName_BeforeInitialize_IsStableNonEmpty()
    {
        var fx = new FileSystemFixture();
        var a = fx.ContainerName;
        var b = fx.ContainerName;
        await Assert.That(a).IsNotNullOrEmpty();
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new FileSystemFixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
