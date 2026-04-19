using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.FileSystem.Builder;
using Rig.TUnit.Storage.FileSystem.Fixtures;
using Rig.TUnit.Storage.FileSystem.Helpers;

namespace Rig.TUnit.Storage.FileSystem.Tests.Integration;

public sealed class UseFileSystemFluentTests
{
    [Test]
    public async Task UseFileSystem_RegistersBuilder_WithoutException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("/tmp/rig-fs-integ");

        FileSystemRigBuilder? configured = null;
        captured!.UseFileSystem(source, b => configured = b);

        await Assert.That(configured).IsNotNull();
    }

    [Test]
    public async Task Fixture_Initialize_CreatesSandboxRoot_ThenRoundtripsFile()
    {
        await using var fx = new FileSystemFixture();
        await fx.InitializeAsync();

        await Assert.That(Directory.Exists(fx.Root)).IsTrue();

        var path = Path.Combine(fx.Root, "hello.txt");
        await File.WriteAllTextAsync(path, "hello");
        var body = await File.ReadAllTextAsync(path);

        await Assert.That(body).IsEqualTo("hello");
    }

    [Test]
    public async Task PathSandboxHelper_Resolve_StaysWithinRoot_ForLegitimatePaths()
    {
        await using var fx = new FileSystemFixture();
        await fx.InitializeAsync();

        var resolved = PathSandboxHelper.Resolve(fx.Root, "sub/file.txt");

        await Assert.That(resolved.StartsWith(fx.Root, StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task PathSandboxHelper_Resolve_BlocksDotDotEscape()
    {
        await using var fx = new FileSystemFixture();
        await fx.InitializeAsync();

        await Assert.That(() => PathSandboxHelper.Resolve(fx.Root, "../escape.txt"))
            .ThrowsExactly<UnauthorizedAccessException>();
    }
}
