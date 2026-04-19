using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;
using Rig.TUnit.Storage.S3.Builder;

namespace Rig.TUnit.Storage.S3.Tests.Unit;

public sealed class S3RigBuilderTests
{
    [Test]
    public async Task S3RigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(S3RigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task S3RigBuilder_TypeMetadata_InheritsStorageRigBuilderCrtp()
    {
        var baseType = typeof(S3RigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(StorageRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(S3RigBuilder));
    }

    [Test]
    public async Task S3RigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("http://localhost:4566");
        await Assert.That(() => new S3RigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task S3RigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new S3RigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
