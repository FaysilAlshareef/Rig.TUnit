# Rig.TUnit.Security.OAuth

In-process mock OAuth 2.0 / OpenID Connect server. Exposes `/authorize`, `/token`,
`/jwks`, and `/.well-known/openid-configuration`. Supports client-credentials,
authorization-code + PKCE (S256), and refresh-token flows.

## Install

```xml
<PackageReference Include="Rig.TUnit.Security.OAuth" />
```

## Example

```csharp
await using var mock = new MockOAuthServer(new MockOAuthServerOptions { Issuer = "https://mock" });
await mock.StartAsync();

var api = await StartProtectedApiAsync(mock.BaseUrl, mock.Issuer);
var token = await FetchTokenAsync(mock.BaseUrl, grant: "client_credentials");

var resp = await api.Client.GetAsync("/", withBearer: token);
// resp.StatusCode == 200 — token accepted by a real JwtBearerHandler via JWKS.
```

## Dependencies
- `Rig.TUnit.Security`, `Rig.TUnit.Security.Jwt`
- `Microsoft.AspNetCore.App` framework reference

Spec: `003-rig-tunit-ecosystem-expansion` — FR:091, FR:093.
