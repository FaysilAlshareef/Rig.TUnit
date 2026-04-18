using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.NoSql.Dynamo.Builder;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

/// <summary>
/// T030-RED unit tests for <see cref="DynamoRigBuilderExtensions.UseDynamo"/> —
/// null-guards, fluent chain, configure-invocation semantics.
/// </summary>
public sealed class UseDynamoExtensionsTests
{
    private const string SampleConnectionString = "http://localhost:4566";

    [Test]
    public async Task UseDynamo_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => ((RigBuilder)null!).UseDynamo(source, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseDynamo_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseDynamo(null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseDynamo_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        await Assert.That(() => captured!.UseDynamo(source, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseDynamo_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);

        var returned = captured!.UseDynamo(source, _ => { });

        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UseDynamo_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        var calls = 0;

        captured!.UseDynamo(source, _ => calls++);

        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UseDynamo_ValidArgs_PassesDynamoRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleConnectionString);
        DynamoRigBuilder? passed = null;

        captured!.UseDynamo(source, b => passed = b);

        await Assert.That(passed).IsNotNull();
    }
}
