namespace Rig.TUnit.Security.Assertions;

/// <summary>
/// Common security assertion entry-point. Provider-specific helpers (JwtAssert,
/// OAuthAssert) extend the <c>Security</c> namespace with fluent chains.
/// </summary>
public static class SecurityAssert
{
    /// <summary>Asserts an HTTP response status indicates authentication failure.</summary>
    public static async Task HttpIsUnauthorized(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if ((int)response.StatusCode != 401)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new SecurityAssertionException(
                $"Expected HTTP 401 Unauthorized but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }
    }

    /// <summary>Asserts an HTTP response status indicates authorization failure.</summary>
    public static async Task HttpIsForbidden(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if ((int)response.StatusCode != 403)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new SecurityAssertionException(
                $"Expected HTTP 403 Forbidden but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }
    }

    /// <summary>Asserts an HTTP response status indicates authenticated success.</summary>
    public static async Task HttpIsAuthenticated(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new SecurityAssertionException(
                $"Expected authenticated success (<400) but got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }
    }
}

public sealed class SecurityAssertionException : Exception
{
    public SecurityAssertionException(string message) : base(message) { }
}
