using System.IO.Abstractions;
using Rig.TUnit.Storage.Fixtures;

namespace Rig.TUnit.Storage.FileSystem.Fixtures;

/// <summary>
/// In-process filesystem-backed storage fixture — uses System.IO.Abstractions so
/// tests can target <see cref="IFileSystem"/> (mockable) while the real
/// <see cref="FileSystem"/> is used at runtime. Creates a unique temp directory
/// per fixture; cleans it up on disposal.
/// </summary>
public sealed class FileSystemFixture : StorageFixtureBase
{
    private string? _root;

    public IFileSystem FileSystem { get; } = new System.IO.Abstractions.FileSystem();

    public string Root => _root ?? throw new InvalidOperationException("InitializeAsync must run first.");

    public override string ConnectionString => Root;

    public override Task InitializeAsync()
    {
        if (_root is not null) return Task.CompletedTask;
        _root = Path.Combine(Path.GetTempPath(), "rigtunit-fs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        if (_root is not null && Directory.Exists(_root))
        {
            try { Directory.Delete(_root, recursive: true); }
            catch (IOException) { /* locked by another handle — best effort */ }
            catch (UnauthorizedAccessException) { /* permission — best effort */ }
        }
        _root = null;
        return ValueTask.CompletedTask;
    }
}
