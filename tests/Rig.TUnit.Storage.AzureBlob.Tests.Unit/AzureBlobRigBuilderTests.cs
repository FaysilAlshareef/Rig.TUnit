using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.AzureBlob.Builder;
using Rig.TUnit.Storage.Builder;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

public sealed class AzureBlobRigBuilderTests
{
    [Test]
    public async Task AzureBlobRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(AzureBlobRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task AzureBlobRigBuilder_TypeMetadata_InheritsStorageRigBuilderCrtp()
    {
        var baseType = typeof(AzureBlobRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(StorageRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(AzureBlobRigBuilder));
    }

    [Test]
    public async Task AzureBlobRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("UseDevelopmentStorage=true");
        await Assert.That(() => new AzureBlobRigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AzureBlobRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new AzureBlobRigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
