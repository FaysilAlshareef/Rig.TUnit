using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.AzureBlob.Builder;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

public sealed class UseAzureBlobExtensionsTests
{
    private const string SampleConnectionString = "UseDevelopmentStorage=true";

    [Test]
    public async Task UseAzureBlob_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => ((RigBuilder)null!).UseAzureBlob(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseAzureBlob_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UseAzureBlob(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseAzureBlob_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        await Assert.That(() => captured!.UseAzureBlob(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseAzureBlob_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var returned = captured!.UseAzureBlob(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseAzureBlob_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;
        captured!.UseAzureBlob(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseAzureBlob_ValidArgs_PassesAzureBlobRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        AzureBlobRigBuilder? passed = null;
        captured!.UseAzureBlob(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
