using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.FileSystem.Builder;

namespace Rig.TUnit.Storage.FileSystem.Tests.Unit;

public sealed class FileSystemRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("/tmp/rig-fs-a");
        FileSystemRigBuilder? built = null;
        captured!.UseFileSystem(source, b => built = b);
        await Assert.That(built!.ConnectionString).IsEqualTo("/tmp/rig-fs-a");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("/tmp/rig-fs-b");
        var built = new FileSystemRigBuilder(captured!, source);
        await Assert.That(built.ConnectionString).IsEqualTo("/tmp/rig-fs-b");
    }
}
