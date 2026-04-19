namespace Rig.TUnit.Microservices.Contracts.Fixtures;

/// <summary>
/// Provider-side verification helper — given a loaded <see cref="ContractPact"/>
/// and an HTTP interaction simulator, asserts the provider satisfies every
/// documented interaction. Simulator is a delegate so tests can mock producer
/// endpoints without spinning up a real web host.
/// </summary>
public sealed class ProviderVerificationFixture
{
    private readonly ContractPact _pact;
    private readonly Func<ContractInteraction, Task<(int Status, string? Body)>> _simulator;

    public ProviderVerificationFixture(
        ContractPact pact,
        Func<ContractInteraction, Task<(int Status, string? Body)>> simulator)
    {
        _pact = pact ?? throw new ArgumentNullException(nameof(pact));
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
    }

    public async Task<ProviderVerificationReport> VerifyAllAsync(CancellationToken ct = default)
    {
        var failures = new List<string>();

        foreach (var interaction in _pact.Interactions)
        {
            ct.ThrowIfCancellationRequested();

            var (status, body) = await _simulator(interaction).ConfigureAwait(false);

            if (status != interaction.ResponseStatus)
            {
                failures.Add(
                    $"[{interaction.Description}] Expected status {interaction.ResponseStatus} but got {status}.");
                continue;
            }

            if (interaction.ResponseBody is { } expectedBody && !string.Equals(expectedBody, body, StringComparison.Ordinal))
            {
                failures.Add(
                    $"[{interaction.Description}] Expected body '{expectedBody}' but got '{body ?? "<null>"}'.");
            }
        }

        return new ProviderVerificationReport(_pact.Interactions.Count - failures.Count, failures);
    }
}

public sealed record ProviderVerificationReport(int Verified, IReadOnlyList<string> Failures)
{
    public bool Success => Failures.Count == 0;
}
