using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Rig.TUnit.Security.Jwt.Options;

namespace Rig.TUnit.Security.Jwt;

/// <summary>
/// Fluent JWT builder. Produces tokens acceptable by a real
/// <c>JwtBearerHandler</c> (no bypass). Supports HS256 / RS256 signing with kid rotation.
/// </summary>
public sealed class JwtBuilder
{
    private readonly JwtBuilderOptions _options;
    private string? _issuer;
    private string? _audience;
    private readonly List<Claim> _claims = new();
    private DateTimeOffset? _notBefore;
    private DateTimeOffset? _expires;
    private SigningCredentials? _creds;
    private string? _kid;

    public JwtBuilder(JwtBuilderOptions? options = null)
    {
        _options = options ?? new JwtBuilderOptions { DefaultIssuer = "rigtunit", DefaultAudience = "rigtunit" };
    }

    public static JwtBuilder Create(JwtBuilderOptions? options = null) => new(options);

    public JwtBuilder Issuer(string issuer) { _issuer = issuer; return this; }
    public JwtBuilder Audience(string audience) { _audience = audience; return this; }
    public JwtBuilder Claim(string type, string value) { _claims.Add(new Claim(type, value)); return this; }
    public JwtBuilder Subject(string sub) => Claim(JwtRegisteredClaimNames.Sub, sub);

    public JwtBuilder ExpiresIn(TimeSpan lifetime)
    {
        _notBefore ??= DateTimeOffset.UtcNow;
        _expires = _notBefore.Value.Add(lifetime);
        return this;
    }

    public JwtBuilder NotBefore(DateTimeOffset nbf) { _notBefore = nbf; return this; }
    public JwtBuilder ExpiresAt(DateTimeOffset exp) { _expires = exp; return this; }
    public JwtBuilder WithKid(string kid) { _kid = kid; return this; }

    public JwtBuilder SignedWithHs256(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length < 32) throw new ArgumentException("HS256 key must be at least 32 bytes.", nameof(key));
        var secKey = new SymmetricSecurityKey(key) { KeyId = _kid };
        _creds = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256);
        return this;
    }

    public JwtBuilder SignedWithHs256(string keyAscii) => SignedWithHs256(System.Text.Encoding.UTF8.GetBytes(keyAscii));

    public JwtBuilder SignedWithRs256(X509Certificate2 cert)
    {
        ArgumentNullException.ThrowIfNull(cert);
        var secKey = new X509SecurityKey(cert) { KeyId = _kid ?? cert.Thumbprint };
        _creds = new SigningCredentials(secKey, SecurityAlgorithms.RsaSha256);
        return this;
    }

    public JwtBuilder SignedWithRs256(RSA rsa, string? kid = null)
    {
        ArgumentNullException.ThrowIfNull(rsa);
        var secKey = new RsaSecurityKey(rsa) { KeyId = kid ?? _kid };
        _creds = new SigningCredentials(secKey, SecurityAlgorithms.RsaSha256);
        return this;
    }

    public string Build()
    {
        if (_creds is null) throw new InvalidOperationException("Call SignedWithHs256 or SignedWithRs256 before Build().");
        var now = DateTimeOffset.UtcNow;
        var nbf = _notBefore ?? now;
        var exp = _expires ?? nbf.AddMinutes(_options.DefaultLifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: _issuer ?? _options.DefaultIssuer,
            audience: _audience ?? _options.DefaultAudience,
            claims: _claims,
            notBefore: nbf.UtcDateTime,
            expires: exp.UtcDateTime,
            signingCredentials: _creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ─── Negative builders (T252) ────────────────────────────────────────────

    public string BuildExpired(TimeSpan ago)
    {
        _notBefore = DateTimeOffset.UtcNow - ago - TimeSpan.FromMinutes(5);
        _expires = DateTimeOffset.UtcNow - ago;
        return Build();
    }

    public string BuildNotYetValid(TimeSpan inTheFuture)
    {
        _notBefore = DateTimeOffset.UtcNow + inTheFuture;
        _expires = _notBefore.Value + TimeSpan.FromMinutes(_options.DefaultLifetimeMinutes);
        return Build();
    }

    /// <summary>Produces a token whose signature byte-array is flipped so validation fails.</summary>
    public string BuildTampered()
    {
        var jwt = Build();
        var parts = jwt.Split('.');
        if (parts.Length != 3) return jwt;
        // Flip a byte in the signature segment. Keep it URL-safe base64.
        var sig = parts[2];
        var flipped = sig.Length > 0
            ? (sig[0] == 'A' ? 'B' : 'A') + sig[1..]
            : sig;
        return $"{parts[0]}.{parts[1]}.{flipped}";
    }
}
