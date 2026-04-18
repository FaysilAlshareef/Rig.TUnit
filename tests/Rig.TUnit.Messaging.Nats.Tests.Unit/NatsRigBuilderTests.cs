using Microsoft.Extensions.DependencyInjection;
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
}
