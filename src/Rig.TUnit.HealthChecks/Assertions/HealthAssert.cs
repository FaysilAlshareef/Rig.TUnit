using System.Diagnostics;
using System.Text.Json;

namespace Rig.TUnit.HealthChecks.Assertions;

/// <summary>
/// Fluent assertions over an ASP.NET Core health-check endpoint. Expects the
/// JSON payload produced by <c>UseHealthChecks(...)</c> with the default JSON
/// writer (status + entries). Compatible with the pattern:
///
/// <code>
/// app.MapHealthChecks("/health/ready", new() {
///   ResponseWriter = (ctx, r) => ctx.Response.WriteAsJsonAsync(new {
///     status = r.Status.ToString(),
///     entries = r.Entries.ToDictionary(kv => kv.Key, kv => new { status = kv.Value.Status.ToString() })
///   })
/// });
/// </code>
/// </summary>
public sealed class HealthAssert
{
    private readonly HttpClient _client;
    private readonly string _path;

    private HealthAssert(HttpClient client, string path)
    {
        _client = client;
        _path = path;
    }

    public static HealthAssert On(HttpClient client, string path)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new HealthAssert(client, path);
    }

    public async Task IsHealthy()
    {
        var doc = await FetchAsync();
        var status = doc.RootElement.GetProperty("status").GetString();
        if (status != "Healthy")
        {
            throw new HealthAssertionException($"Expected Healthy but got {status}.");
        }
    }

    public async Task IsUnhealthy()
    {
        var doc = await FetchAsync();
        var status = doc.RootElement.GetProperty("status").GetString();
        if (status != "Unhealthy")
        {
            throw new HealthAssertionException($"Expected Unhealthy but got {status}.");
        }
    }

    public async Task Contains(string dependency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependency);
        var doc = await FetchAsync();
        if (!doc.RootElement.GetProperty("entries").TryGetProperty(dependency, out _))
        {
            throw new HealthAssertionException($"Health payload missing dependency '{dependency}'.");
        }
    }

    public async Task InTime(TimeSpan budget)
    {
        var sw = Stopwatch.StartNew();
        _ = await _client.GetAsync(_path);
        sw.Stop();
        if (sw.Elapsed > budget)
        {
            throw new HealthAssertionException($"Health check exceeded budget: {sw.Elapsed} > {budget}.");
        }
    }

    private async Task<JsonDocument> FetchAsync()
    {
        var resp = await _client.GetAsync(_path);
        var body = await resp.Content.ReadAsStringAsync();
        try { return JsonDocument.Parse(body); }
        catch (JsonException ex) { throw new HealthAssertionException($"Health payload is not valid JSON: {ex.Message}. Body: {body}"); }
    }
}

public sealed class HealthAssertionException : Exception
{
    public HealthAssertionException(string message) : base(message) { }
}

/// <summary>Runtime-toggleable dependency-health flag used to simulate outages.</summary>
public sealed class DependencyDownSimulator
{
    private int _down;
    public bool IsDown => Volatile.Read(ref _down) == 1;
    public void GoDown() => Interlocked.Exchange(ref _down, 1);
    public void Recover() => Interlocked.Exchange(ref _down, 0);
}

/// <summary>
/// Probe distinguisher — live / ready / startup have different semantics:
/// live = process up, ready = can serve traffic, startup = initialisation complete.
/// </summary>
public enum ProbeKind { Live, Ready, Startup }
