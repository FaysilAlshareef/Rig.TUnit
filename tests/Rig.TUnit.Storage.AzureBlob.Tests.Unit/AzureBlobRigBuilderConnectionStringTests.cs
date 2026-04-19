using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.AzureBlob.Builder;

namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

public sealed class AzureBlobRigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("DefaultEndpointsProtocol=https;AccountName=test");
        AzureBlobRigBuilder? built = null;
        captured!.UseAzureBlob(source, b => built = b);
        await Assert.That(built!.ConnectionString).IsEqualTo("DefaultEndpointsProtocol=https;AccountName=test");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("UseDevelopmentStorage=true");
        var built = new AzureBlobRigBuilder(captured!, source);
        await Assert.That(built.ConnectionString).IsEqualTo("UseDevelopmentStorage=true");
    }
}
