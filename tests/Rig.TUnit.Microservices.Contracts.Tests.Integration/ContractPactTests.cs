using Rig.TUnit.Microservices.Contracts;

namespace Rig.TUnit.Microservices.Contracts.Tests.Integration;

public sealed class ContractPactTests
{
    [Test]
    public async Task VerifyContract_WhenPactsMatch_DoesNotThrow()
    {
        var pact = new ContractPact(
            "shop-ui",
            "orders-api",
            new[]
            {
                new ContractInteraction("GET orders", "GET", "/orders", null, null, 200, null, "[]"),
            });

        ContractAssert.VerifyContract(pact, pact);
        await Task.Yield();
    }

    [Test]
    public async Task VerifyContract_WhenPathDiffers_ThrowsException()
    {
        var expected = new ContractPact("ui", "api", new[] { new ContractInteraction("d", "GET", "/a") });
        var actual = new ContractPact("ui", "api", new[] { new ContractInteraction("d", "GET", "/b") });

        var threw = false;
        try { ContractAssert.VerifyContract(expected, actual); }
        catch (ContractAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task VerifyContract_WhenInteractionCountMismatches_ThrowsException()
    {
        var expected = new ContractPact("ui", "api", new[] { new ContractInteraction("d1", "GET", "/a") });
        var actual = new ContractPact("ui", "api", new[]
        {
            new ContractInteraction("d1", "GET", "/a"),
            new ContractInteraction("d2", "POST", "/b"),
        });

        var threw = false;
        try { ContractAssert.VerifyContract(expected, actual); }
        catch (ContractAssertionException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }
}
