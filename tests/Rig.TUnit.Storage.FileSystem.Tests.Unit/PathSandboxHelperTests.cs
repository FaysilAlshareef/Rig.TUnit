using Rig.TUnit.Storage.FileSystem.Helpers;

namespace Rig.TUnit.Storage.FileSystem.Tests.Unit;

/// <summary>
/// Pure-function tests for <see cref="PathSandboxHelper"/> — path-traversal prevention
/// + sandbox boundary enforcement. No real filesystem writes; these are path-string tests.
/// </summary>
public sealed class PathSandboxHelperTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "rigtunit-sandbox-test");

    [Test]
    public async Task Resolve_WithSimpleRelativePath_ReturnsJoinedPath()
    {
        var actual = PathSandboxHelper.Resolve(Root, "file.txt");
        var expected = Path.GetFullPath(Path.Combine(Root, "file.txt"));
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Resolve_WithNestedRelativePath_ReturnsJoinedPath()
    {
        var actual = PathSandboxHelper.Resolve(Root, "sub/dir/file.txt");
        var expected = Path.GetFullPath(Path.Combine(Root, "sub", "dir", "file.txt"));
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Resolve_WithDotDotEscape_ThrowsUnauthorizedAccessException()
    {
        await Assert.That(() => PathSandboxHelper.Resolve(Root, "../outside.txt"))
            .ThrowsExactly<UnauthorizedAccessException>();
    }

    [Test]
    public async Task Resolve_WithDeepDotDotEscape_ThrowsUnauthorizedAccessException()
    {
        await Assert.That(() => PathSandboxHelper.Resolve(Root, "sub/../../../outside.txt"))
            .ThrowsExactly<UnauthorizedAccessException>();
    }

    [Test]
    public async Task Resolve_WithAbsolutePathOutsideRoot_ThrowsUnauthorizedAccessException()
    {
        var outside = Path.Combine(Path.GetTempPath(), "elsewhere", "file.txt");
        await Assert.That(() => PathSandboxHelper.Resolve(Root, outside))
            .ThrowsExactly<UnauthorizedAccessException>();
    }

    [Test]
    public async Task Resolve_WithNullRoot_ThrowsArgumentException()
    {
        await Assert.That(() => PathSandboxHelper.Resolve(null!, "file.txt"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_WithEmptyRelativePath_ThrowsArgumentException()
    {
        await Assert.That(() => PathSandboxHelper.Resolve(Root, ""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task IsInside_WhenPathInsideRoot_ReturnsTrue()
    {
        var inside = Path.Combine(Root, "a", "b.txt");
        await Assert.That(PathSandboxHelper.IsInside(Root, inside)).IsTrue();
    }

    [Test]
    public async Task IsInside_WhenPathOutsideRoot_ReturnsFalse()
    {
        var outside = Path.Combine(Path.GetTempPath(), "elsewhere.txt");
        await Assert.That(PathSandboxHelper.IsInside(Root, outside)).IsFalse();
    }
}
