using Rig.TUnit.Storage.FileSystem.Fixtures;

namespace Rig.TUnit.Storage.FileSystem.Tests.Integration;

public sealed class FileSystemQuirkTests
{
    [Test]
    public async Task WriteAndRead_RoundtripsTextFile()
    {
        await using var fx = new FileSystemFixture();
        await fx.InitializeAsync();
        var path = Path.Combine(fx.Root, "hello.txt");

        await File.WriteAllTextAsync(path, "hello world");
        var content = await File.ReadAllTextAsync(path);

        await Assert.That(content).IsEqualTo("hello world");
    }

    [Test]
    public async Task Dispose_CleansUpRootDirectory()
    {
        var fx = new FileSystemFixture();
        await fx.InitializeAsync();
        var root = fx.Root;
        await File.WriteAllTextAsync(Path.Combine(root, "x.txt"), "x");
        await fx.DisposeAsync();

        await Assert.That(Directory.Exists(root)).IsFalse();
    }

    [Test]
    public async Task TwoFixtures_UseDistinctRootDirectories()
    {
        await using var a = new FileSystemFixture();
        await using var b = new FileSystemFixture();
        await a.InitializeAsync();
        await b.InitializeAsync();

        await Assert.That(a.Root).IsNotEqualTo(b.Root);
    }
}
