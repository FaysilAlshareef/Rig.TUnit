using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.Kafka.Builder;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 unit tests for <see cref="KafkaRigBuilder"/> — CRTP shape, sealed, null-guards.
/// </summary>
public sealed class KafkaRigBuilderTests
{
    [Test]
    public async Task KafkaRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(KafkaRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task KafkaRigBuilder_TypeMetadata_InheritsMessagingRigBuilderCrtp()
    {
        var baseType = typeof(KafkaRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(MessagingRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(KafkaRigBuilder));
    }

    [Test]
    public async Task KafkaRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("localhost:9092");

        await Assert.That(() => new KafkaRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task KafkaRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new KafkaRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
