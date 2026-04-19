using System.Text.Json;

namespace Rig.TUnit.Microservices.Contracts.Helpers;

/// <summary>
/// File-based Pact broker stub — reads JSON pact documents from a local
/// directory rather than calling an HTTP broker. Intentionally minimal
/// (per C-002 — no HAL emulation, no HTTP). Consumers that need a real
/// broker should plug in their own adapter.
/// </summary>
public sealed class PactBrokerClientStub
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _pactsDirectory;

    public PactBrokerClientStub(string pactsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pactsDirectory);
        _pactsDirectory = pactsDirectory;
    }

    /// <summary>
    /// Load a ContractPact from a JSON file on disk.
    /// Expected filename convention: <c>{consumer}-{provider}.json</c>.
    /// </summary>
    public ContractPact Load(string consumerName, string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var path = Path.Combine(_pactsDirectory, $"{consumerName}-{providerName}.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Pact not found: {path}", path);
        }

        var json = File.ReadAllText(path);
        var pact = JsonSerializer.Deserialize<ContractPact>(json, Options);
        if (pact is null)
        {
            throw new InvalidOperationException($"Pact file '{path}' deserialized to null — the document may be empty or malformed.");
        }
        return pact;
    }

    /// <summary>Enumerate every pact the stub can see in its directory.</summary>
    public IReadOnlyList<string> DiscoverPacts()
    {
        if (!Directory.Exists(_pactsDirectory)) return Array.Empty<string>();
        return Directory.EnumerateFiles(_pactsDirectory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .ToArray();
    }
}
