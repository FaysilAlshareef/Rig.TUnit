using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.ServiceBus.Builder;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 coverage-lifting test for <see cref="ServiceBusRigBuilder.ConnectionString"/>.
/// The metadata assertions in <see cref="ServiceBusRigBuilderTests"/> cover the sealed +
/// CRTP shape but never execute the <c>ConnectionString</c> getter. This suite drives
/// the property with distinct sources.
/// </summary>
public sealed class ServiceBusRigBuilderConnectionStringTests
{
    private const string SampleConnectionString =
        "Endpoint=sb://unit-test/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==";

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var builder = new ServiceBusRigBuilder(captured!, source);

        var connectionString = builder.ConnectionString;

        await Assert.That(connectionString).IsEqualTo(SampleConnectionString);
    }

    [Test]
    public async Task ConnectionString_DifferentSources_ReturnDistinctValues()
    {
        var services = new ServiceCollection();
        RigBuilder? captured = null;
        services.AddRigTUnit(rig => captured = rig);

        var a = new ServiceBusRigBuilder(captured!, RigConnect.FromValue("Endpoint=sb://a/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA=="));
        var b = new ServiceBusRigBuilder(captured!, RigConnect.FromValue("Endpoint=sb://b/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA=="));

        var aStr = a.ConnectionString;
        var bStr = b.ConnectionString;

        await Assert.That(aStr).IsNotEqualTo(bStr);
    }
}
