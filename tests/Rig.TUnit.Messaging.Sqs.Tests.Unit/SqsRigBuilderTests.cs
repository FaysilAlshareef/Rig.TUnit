using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Messaging.Builder;
using Rig.TUnit.Messaging.Sqs.Builder;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

public sealed class SqsRigBuilderTests
{
    [Test]
    public async Task SqsRigBuilder_TypeMetadata_IsSealed()
    {
        await Assert.That(typeof(SqsRigBuilder).IsSealed).IsTrue();
    }

    [Test]
    public async Task SqsRigBuilder_TypeMetadata_InheritsMessagingRigBuilderCrtp()
    {
        var baseType = typeof(SqsRigBuilder).BaseType;

        await Assert.That(baseType).IsNotNull();
        await Assert.That(baseType!.IsGenericType).IsTrue();
        await Assert.That(baseType!.GetGenericTypeDefinition()).IsEqualTo(typeof(MessagingRigBuilder<>));
        await Assert.That(baseType!.GenericTypeArguments[0]).IsEqualTo(typeof(SqsRigBuilder));
    }

    [Test]
    public async Task SqsRigBuilder_Ctor_NullRoot_Throws()
    {
        var source = RigConnect.FromValue("http://localhost:4566");

        await Assert.That(() => new SqsRigBuilder(null!, source))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task SqsRigBuilder_Ctor_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => new SqsRigBuilder(captured!, null!))
            .ThrowsExactly<ArgumentNullException>();
    }
}
