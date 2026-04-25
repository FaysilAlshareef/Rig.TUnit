using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.ServiceBus.Builder;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// FR-035 backfill unit tests for <see cref="ServiceBusRigBuilder"/> — CRTP shape,
/// seal, constructor null-guards. No container, no infrastructure.
/// </summary>
public sealed class ServiceBusRigBuilderTests
{
    [Test]
    public async Task ServiceBusRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(ServiceBusRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task ServiceBusRigBuilder_TypeMetadata_InheritsMessagingRigBuilderCrtp()
    {
        var baseType = typeof(ServiceBusRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(MessagingRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(ServiceBusRigBuilder));
    }

    [Test]
    public async Task ServiceBusRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("Endpoint=sb://local/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==");

        await Assert.That(() => new ServiceBusRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ServiceBusRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new ServiceBusRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task WithTopology_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Endpoint=sb://local/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==");
        var builder = new ServiceBusRigBuilder(captured!, source);

        await Assert.That(() => builder.WithTopology(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task WithTopology_ValidConfigure_ReturnsBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Endpoint=sb://local/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==");
        var builder = new ServiceBusRigBuilder(captured!, source);

        var returned = builder.WithTopology(_ => { });

        await Assert.That(returned).IsSameReferenceAs(builder);
    }

    [Test]
    public async Task ApplyTopologyAsync_NoTopologyConfigured_DoesNothing(CancellationToken ct)
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("Endpoint=sb://local/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==");
        var builder = new ServiceBusRigBuilder(captured!, source);

        await Assert.That(async () => await builder.ApplyTopologyAsync(ct))
            .ThrowsNothing();
    }

    [Test]
    public async Task ConnectionString_ValueSource_ReturnsRawValue()
    {
        const string connStr = "Endpoint=sb://local/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==";
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(connStr);
        var builder = new ServiceBusRigBuilder(captured!, source);

        await Assert.That(builder.ConnectionString).IsEqualTo(connStr);
    }
}
