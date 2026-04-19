using Rig.TUnit.Microservices.Contracts;
using Rig.TUnit.Microservices.Contracts.Helpers;

namespace Rig.TUnit.Microservices.Contracts.Tests.Unit;

public sealed class ProviderVerificationHarnessTests
{
    private static ContractPact SamplePact(params ContractInteraction[] interactions)
        => new("c", "p", interactions);

    [Test]
    public async Task Ctor_NullPact_Throws()
    {
        await Assert.That(() => new ProviderVerificationHarness(null!, _ => Task.FromResult((200, (string?)null))))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_NullSimulator_Throws()
    {
        await Assert.That(() => new ProviderVerificationHarness(SamplePact(), null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task VerifyAllAsync_StatusMatches_Succeeds()
    {
        var pact = SamplePact(new ContractInteraction("list", "GET", "/x", ResponseStatus: 200));
        var fixture = new ProviderVerificationHarness(pact, _ => Task.FromResult((200, (string?)null)));

        var report = await fixture.VerifyAllAsync();

        await Assert.That(report.Success).IsTrue();
        await Assert.That(report.Verified).IsEqualTo(1);
    }

    [Test]
    public async Task VerifyAllAsync_StatusMismatch_FailsReport()
    {
        var pact = SamplePact(new ContractInteraction("list", "GET", "/x", ResponseStatus: 200));
        var fixture = new ProviderVerificationHarness(pact, _ => Task.FromResult((500, (string?)null)));

        var report = await fixture.VerifyAllAsync();

        await Assert.That(report.Success).IsFalse();
        await Assert.That(report.Failures.Count).IsEqualTo(1);
    }

    [Test]
    public async Task VerifyAllAsync_BodyMismatch_FailsReport()
    {
        var pact = SamplePact(new ContractInteraction("list", "GET", "/x", ResponseStatus: 200, ResponseBody: "OK"));
        var fixture = new ProviderVerificationHarness(pact, _ => Task.FromResult((200, (string?)"WRONG")));

        var report = await fixture.VerifyAllAsync();

        await Assert.That(report.Success).IsFalse();
    }

    [Test]
    public async Task VerifyAllAsync_MultipleInteractions_AllVerified()
    {
        var pact = SamplePact(
            new ContractInteraction("a", "GET", "/a", ResponseStatus: 200),
            new ContractInteraction("b", "GET", "/b", ResponseStatus: 204));
        var fixture = new ProviderVerificationHarness(pact, i =>
            Task.FromResult((i.ResponseStatus, (string?)null)));

        var report = await fixture.VerifyAllAsync();

        await Assert.That(report.Verified).IsEqualTo(2);
    }
}
