namespace Rig.TUnit.Microservices.Contracts;

/// <summary>
/// Pact-style contract: Consumer + Provider agree on request/response shape.
/// Both sides test independently — consumer tests feed expected contracts,
/// provider tests verify they still satisfy them.
/// </summary>
public sealed record ContractPact(
    string ConsumerName,
    string ProviderName,
    IReadOnlyList<ContractInteraction> Interactions);

public sealed record ContractInteraction(
    string Description,
    string Method,
    string Path,
    IReadOnlyDictionary<string, string>? RequestHeaders = null,
    string? RequestBody = null,
    int ResponseStatus = 200,
    IReadOnlyDictionary<string, string>? ResponseHeaders = null,
    string? ResponseBody = null);

public static class ContractAssert
{
    public static void VerifyContract(ContractPact expected, ContractPact actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (expected.ConsumerName != actual.ConsumerName || expected.ProviderName != actual.ProviderName)
        {
            throw new ContractAssertionException(
                $"Pact identity mismatch — expected {expected.ConsumerName}->{expected.ProviderName}, got {actual.ConsumerName}->{actual.ProviderName}.");
        }

        if (expected.Interactions.Count != actual.Interactions.Count)
        {
            throw new ContractAssertionException(
                $"Interaction count mismatch — expected {expected.Interactions.Count}, got {actual.Interactions.Count}.");
        }

        foreach (var pair in expected.Interactions.Zip(actual.Interactions))
        {
            var (e, a) = pair;
            if (e.Method != a.Method || e.Path != a.Path)
            {
                throw new ContractAssertionException(
                    $"Interaction '{e.Description}' route mismatch — expected {e.Method} {e.Path}, got {a.Method} {a.Path}.");
            }
            if (e.ResponseStatus != a.ResponseStatus)
            {
                throw new ContractAssertionException(
                    $"Interaction '{e.Description}' response status — expected {e.ResponseStatus}, got {a.ResponseStatus}.");
            }
        }
    }
}

public sealed class ContractAssertionException : Exception
{
    public ContractAssertionException(string message) : base(message) { }
}
