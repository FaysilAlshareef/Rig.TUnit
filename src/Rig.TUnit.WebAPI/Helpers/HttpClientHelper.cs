using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Rig.TUnit.WebAPI.Helpers;

/// <summary>
/// Test infrastructure helper that wraps a <see cref="WebApplicationFactory{TEntryPoint}"/>
/// and exposes convenience methods for sending HTTP requests and deserializing JSON responses.
/// </summary>
/// <typeparam name="TProgram">The entry-point type of the application under test.</typeparam>
public sealed class HttpClientHelper<TProgram> : IAsyncDisposable
    where TProgram : class
{
    private readonly WebApplicationFactory<TProgram> _factory;
    private HttpClient? _client;

    public HttpClientHelper(WebApplicationFactory<TProgram> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    /// <summary>
    /// Lazily-created default <see cref="HttpClient"/> routed through the test server.
    /// Subsequent accesses return the same instance until the helper is disposed.
    /// </summary>
    public HttpClient Client => _client ??= _factory.CreateClient();

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> with optional configuration. Each call returns a fresh client.
    /// </summary>
    public HttpClient CreateClient(Action<WebApplicationFactoryClientOptions>? configure = null)
    {
        var options = new WebApplicationFactoryClientOptions();
        configure?.Invoke(options);
        return _factory.CreateClient(options);
    }

    /// <summary>
    /// Sets the default <c>Authorization</c> header on <see cref="Client"/> to the supplied bearer token.
    /// Pass <see langword="null"/> to clear the header. Returns <see langword="this"/> to support fluent chaining.
    /// <para>
    /// NOTE: <c>TestAuthenticationHandler</c> ignores the Authorization header — use this when the app under test
    /// reads the header directly (for example via <c>HttpContext.Request.Headers</c>) or when forwarding a token
    /// to a real authentication handler registered outside the test helpers.
    /// </para>
    /// </summary>
    public HttpClientHelper<TProgram> WithBearerToken(string? token)
    {
        Client.DefaultRequestHeaders.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
        return this;
    }

    /// <summary>
    /// Sets an arbitrary default header on <see cref="Client"/>. Overwrites any existing value for that header.
    /// </summary>
    public HttpClientHelper<TProgram> WithHeader(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Client.DefaultRequestHeaders.Remove(name);
        Client.DefaultRequestHeaders.Add(name, value);
        return this;
    }

    /// <summary>
    /// Sends a GET request and deserializes the JSON response body.
    /// </summary>
    public async Task<TResponse?> GetAsync<TResponse>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await Client.GetFromJsonAsync<TResponse>(requestUri, cancellationToken);
    }

    /// <summary>
    /// Sends a POST request with a JSON body and deserializes the JSON response body.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync(requestUri, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
    }

    /// <summary>
    /// Sends a PUT request with a JSON body and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    public async Task<HttpResponseMessage> PutAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await Client.PutAsJsonAsync(requestUri, request, cancellationToken);
    }

    /// <summary>
    /// Sends a DELETE request and returns the raw <see cref="HttpResponseMessage"/>.
    /// </summary>
    public async Task<HttpResponseMessage> DeleteAsync(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        return await Client.DeleteAsync(requestUri, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        return ValueTask.CompletedTask;
    }
}
