using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.RabbitMq.Builder;

namespace Rig.TUnit.Messaging.RabbitMq.Tests.Unit;

public sealed class RabbitMqRigBuilderTests
{
    [Test]
    public async Task RabbitMqRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(RabbitMqRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task RabbitMqRigBuilder_TypeMetadata_InheritsMessagingRigBuilderCrtp()
    {
        var baseType = typeof(RabbitMqRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(MessagingRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(RabbitMqRigBuilder));
    }

    [Test]
    public async Task RabbitMqRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("amqp://guest:guest@localhost:5672");

        await Assert.That(() => new RabbitMqRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RabbitMqRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new RabbitMqRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
