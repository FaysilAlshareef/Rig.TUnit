using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Policies.Builder;

namespace Rig.TUnit.Security.Policies.Tests.Unit;

public sealed class UsePoliciesExtensionsTests
{
    private const string SampleScheme = "Test";

    [Test]
    public async Task UsePolicies_NullRig_Throws()
    {
        var source = RigConnect.FromValue(SampleScheme);
        await Assert.That(() => ((RigBuilder)null!).UsePolicies(source, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePolicies_NullSource_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await Assert.That(() => captured!.UsePolicies(null!, _ => { })).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePolicies_NullConfigure_Throws()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleScheme);
        await Assert.That(() => captured!.UsePolicies(source, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UsePolicies_ValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleScheme);
        var returned = captured!.UsePolicies(source, _ => { });
        await Assert.That(returned).IsSameReferenceAs(captured);
    }

    [Test]
    public async Task UsePolicies_ValidArgs_InvokesConfigureOnce()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleScheme);
        var calls = 0;
        captured!.UsePolicies(source, _ => calls++);
        await Assert.That(calls).IsEqualTo(1);
    }

    [Test]
    public async Task UsePolicies_ValidArgs_PassesPolicyRigBuilderInstance()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        var source = RigConnect.FromValue(SampleScheme);
        PolicyRigBuilder? passed = null;
        captured!.UsePolicies(source, b => passed = b);
        await Assert.That(passed).IsNotNull();
    }
}
