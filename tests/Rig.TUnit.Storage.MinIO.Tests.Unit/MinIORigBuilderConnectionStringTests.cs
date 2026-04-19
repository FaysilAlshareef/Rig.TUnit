using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.MinIO.Builder;

namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

public sealed class MinIORigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://minio:9000");
        MinIORigBuilder? built = null;
        captured!.UseMinIO(source, b => built = b);
        await Assert.That(built!.ConnectionString).IsEqualTo("http://minio:9000");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://localhost:9000");
        var built = new MinIORigBuilder(captured!, source);
        await Assert.That(built.ConnectionString).IsEqualTo("http://localhost:9000");
    }
}
