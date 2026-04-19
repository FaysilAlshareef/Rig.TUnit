using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;
using Rig.TUnit.Storage.FileSystem.Builder;

namespace Rig.TUnit.Storage.FileSystem.Tests.Unit;

public sealed class FileSystemRigBuilderTests
{
    [Test]
    public async Task TypeMetadata_WhenChecked_IsSealed()
    {
        await Assert.That(typeof(FileSystemRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task TypeMetadata_WhenInspected_InheritsStorageRigBuilderCrtp()
    {
        var baseType = typeof(FileSystemRigBuilder).BaseType;
        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(StorageRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(FileSystemRigBuilder));
    }

    [Test]
    public async Task Ctor_WithNullRoot_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue("/tmp/rigtunit-fs");
        await Assert.That(() => new FileSystemRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithNullSource_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new FileSystemRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
