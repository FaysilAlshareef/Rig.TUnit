using Microsoft.Extensions.DependencyInjection;
using NATS.Client.JetStream;
using NSubstitute;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.Nats.Builder;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit;

public sealed class NatsRigBuilderTests
{
    [Test]
    public async Task NatsRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(NatsRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task NatsRigBuilder_TypeMetadata_InheritsMessagingRigBuilderCrtp()
    {
        var baseType = typeof(NatsRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(MessagingRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(NatsRigBuilder));
    }

    [Test]
    public async Task NatsRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("nats://localhost:4222");

        await Assert.That(() => new NatsRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task NatsRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new NatsRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task WithTopology_WhenJetStreamContextNull_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("nats://localhost:4222");
        var builder = new NatsRigBuilder(captured!, source);

        await Assert.That(() => builder.WithTopology(t => t.Stream("x", c => c.WithSubjects("x.>"))))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task WithTopology_WhenJetStreamContextProvided_ForwardsCreateStream(CancellationToken ct)
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("nats://localhost:4222");
        var mockJs = Substitute.For<INatsJSContext>();
        var builder = new NatsRigBuilder(captured!, source, mockJs);

        builder.WithTopology(t => t.Stream("orders", c => c.WithSubjects("orders.>")));
        await builder.ApplyTopologyAsync(ct);

        await mockJs.Received(1).CreateStreamAsync(
            Arg.Any<NATS.Client.JetStream.Models.StreamConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyTopologyAsync_WhenNoTopologyConfigured_DoesNothing(CancellationToken ct)
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("nats://localhost:4222");
        var mockJs = Substitute.For<INatsJSContext>();
        var builder = new NatsRigBuilder(captured!, source, mockJs);

        await Assert.That(async () => await builder.ApplyTopologyAsync(ct))
            .ThrowsNothing();
        await mockJs.DidNotReceive().CreateStreamAsync(
            Arg.Any<NATS.Client.JetStream.Models.StreamConfig>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConnectionString_AfterConstruction_ReturnsSourceValue()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue("nats://localhost:4222");
        var builder = new NatsRigBuilder(captured!, source);

        await Assert.That(builder.ConnectionString).IsEqualTo("nats://localhost:4222");
    }
}
