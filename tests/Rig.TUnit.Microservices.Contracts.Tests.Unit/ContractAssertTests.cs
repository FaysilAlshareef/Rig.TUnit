using Rig.TUnit.Microservices.Contracts;

namespace Rig.TUnit.Microservices.Contracts.Tests.Unit;

public sealed class ContractAssertTests
{
    private static ContractPact SimplePact(
        string consumer = "consumer",
        string provider = "provider",
        string method = "GET",
        string path = "/orders",
        int status = 200) =>
        new(consumer, provider, new[]
        {
            new ContractInteraction("list", method, path, ResponseStatus: status),
        });

    // ─── Null guards ─────────────────────────────────────────────────────────

    [Test]
    public async Task VerifyContract_NullExpected_ThrowsArgumentNullException()
    {
        await Assert.That(() => ContractAssert.VerifyContract(null!, SimplePact()))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task VerifyContract_NullActual_ThrowsArgumentNullException()
    {
        await Assert.That(() => ContractAssert.VerifyContract(SimplePact(), null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    // ─── Identity mismatch ───────────────────────────────────────────────────

    [Test]
    public async Task VerifyContract_ConsumerNameMismatch_ThrowsContractAssertionException()
    {
        var expected = SimplePact(consumer: "consumer-a");
        var actual = SimplePact(consumer: "consumer-x");

        await Assert.That(() => ContractAssert.VerifyContract(expected, actual))
            .ThrowsExactly<ContractAssertionException>();
    }

    [Test]
    public async Task VerifyContract_ProviderNameMismatch_ThrowsContractAssertionException()
    {
        var expected = SimplePact(provider: "provider-a");
        var actual = SimplePact(provider: "provider-x");

        await Assert.That(() => ContractAssert.VerifyContract(expected, actual))
            .ThrowsExactly<ContractAssertionException>();
    }

    // ─── Interaction count mismatch ──────────────────────────────────────────

    [Test]
    public async Task VerifyContract_InteractionCountMismatch_ThrowsContractAssertionException()
    {
        var expected = new ContractPact("c", "p", new[]
        {
            new ContractInteraction("get", "GET", "/a"),
            new ContractInteraction("post", "POST", "/b"),
        });
        var actual = new ContractPact("c", "p", new[]
        {
            new ContractInteraction("get", "GET", "/a"),
        });

        await Assert.That(() => ContractAssert.VerifyContract(expected, actual))
            .ThrowsExactly<ContractAssertionException>();
    }

    // ─── Interaction content mismatches ─────────────────────────────────────

    [Test]
    public async Task VerifyContract_MethodMismatch_ThrowsContractAssertionException()
    {
        var expected = SimplePact(method: "GET");
        var actual = SimplePact(method: "POST");

        await Assert.That(() => ContractAssert.VerifyContract(expected, actual))
            .ThrowsExactly<ContractAssertionException>();
    }

    [Test]
    public async Task VerifyContract_PathMismatch_ThrowsContractAssertionException()
    {
        var expected = SimplePact(path: "/orders");
        var actual = SimplePact(path: "/products");

        await Assert.That(() => ContractAssert.VerifyContract(expected, actual))
            .ThrowsExactly<ContractAssertionException>();
    }

    [Test]
    public async Task VerifyContract_ResponseStatusMismatch_ThrowsContractAssertionException()
    {
        var expected = SimplePact(status: 201);
        var actual = SimplePact(status: 200);

        await Assert.That(() => ContractAssert.VerifyContract(expected, actual))
            .ThrowsExactly<ContractAssertionException>();
    }

    // ─── Happy path ──────────────────────────────────────────────────────────

    [Test]
    public Task VerifyContract_MatchingPacts_DoesNotThrow()
    {
        var pact = SimplePact();
        ContractAssert.VerifyContract(pact, pact);
        return Task.CompletedTask;
    }

    // ─── Record property access ──────────────────────────────────────────────

    [Test]
    public async Task ContractPact_Properties_AreAccessible()
    {
        var interactions = new[] { new ContractInteraction("create", "POST", "/items", ResponseStatus: 201) };
        var pact = new ContractPact("my-consumer", "my-provider", interactions);

        await Assert.That(pact.ConsumerName).IsEqualTo("my-consumer");
        await Assert.That(pact.ProviderName).IsEqualTo("my-provider");
        await Assert.That(pact.Interactions.Count).IsEqualTo(1);
        await Assert.That(pact.Interactions[0].Method).IsEqualTo("POST");
        await Assert.That(pact.Interactions[0].ResponseStatus).IsEqualTo(201);
    }

    [Test]
    public async Task ContractAssertionException_Message_IsPreserved()
    {
        var ex = new ContractAssertionException("pact mismatch");
        await Assert.That(ex.Message).IsEqualTo("pact mismatch");
    }
}
