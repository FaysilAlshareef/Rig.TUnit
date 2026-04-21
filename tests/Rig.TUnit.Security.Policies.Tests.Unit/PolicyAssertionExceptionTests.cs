using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Security.Policies;

namespace Rig.TUnit.Security.Policies.Tests.Unit;

public sealed class PolicyAssertionExceptionTests
{
    [Test]
    public async Task PolicyAssertionException_Message_IsPreserved()
    {
        var ex = new PolicyAssertionException("policy denied");

        await Assert.That(ex.Message).IsEqualTo("policy denied");
    }

    [Test]
    public async Task PolicyAssertionException_IsExceptionSubtype()
    {
        var ex = new PolicyAssertionException("x");

        await Assert.That(ex).IsAssignableTo<Exception>();
    }

    [Test]
    public async Task PolicyAssert_Policy_NullServices_ThrowsArgumentNullException()
    {
        await Assert.That(() => PolicyAssert.Policy(null!, "MyPolicy"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task PolicyAssert_Policy_WhiteSpacePolicyName_ThrowsArgumentException()
    {
        var services = new ServiceCollection().BuildServiceProvider();

        await Assert.That(() => PolicyAssert.Policy(services, "  "))
            .ThrowsExactly<ArgumentException>();
    }
}
