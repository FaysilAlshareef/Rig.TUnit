using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Storage.S3.Builder;

namespace Rig.TUnit.Storage.S3.Tests.Unit;

public sealed class S3RigBuilderConnectionStringTests
{
    [Test]
    public async Task ConnectionString_PassesThrough_FromConnectionSource()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://localstack:4566");
        S3RigBuilder? built = null;
        captured!.UseS3(source, b => built = b);
        await Assert.That(built!.ConnectionString).IsEqualTo("http://localstack:4566");
    }

    [Test]
    public async Task ConnectionString_Direct_MatchesSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("http://s3.example.com");
        var built = new S3RigBuilder(captured!, source);
        await Assert.That(built.ConnectionString).IsEqualTo("http://s3.example.com");
    }
}
