using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.Builder;
using Rig.TUnit.Storage.MinIO.Builder;

namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

/// <summary>
/// FR-035 unit tests for <see cref="MinIORigBuilder"/>. Test names use the
/// {Method}_{Scenario}_{ExpectedResult} pattern consistent with all other
/// providers in this repository.
/// </summary>
public sealed class MinIORigBuilderTests
{
    [Test]
    public async Task TypeMetadata_WhenChecked_IsSealed()
    {
        await Assert.That(typeof(MinIORigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task TypeMetadata_WhenInspected_InheritsStorageRigBuilderCrtp()
    {
        var baseType = typeof(MinIORigBuilder).BaseType;
        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(StorageRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(MinIORigBuilder));
    }

    [Test]
    public async Task Ctor_WithNullRoot_ThrowsArgumentNullException()
    {
        var source = RigConnect.FromValue("http://localhost:9000");
        await Assert.That(() => new MinIORigBuilder(null!, source)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithNullSource_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => new MinIORigBuilder(captured!, null!)).ThrowsExactly<ArgumentNullException>();
    }
}
