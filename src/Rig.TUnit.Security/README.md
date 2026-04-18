# Rig.TUnit.Security

Base package for security fixtures (Jwt, OAuth, mTLS). Provides `ISecurityRig`,
`SecurityFixtureBase`, and `SecurityAssert` with HTTP 401 / 403 helpers.

## Install

```xml
<PackageReference Include="Rig.TUnit.Security" />
```

## Example

```csharp
var response = await client.SendAsync(request);
await SecurityAssert.HttpIsUnauthorized(response);
```

Concrete providers: `Rig.TUnit.Security.Jwt`, `Rig.TUnit.Security.OAuth`.

## Dependencies
- `Rig.TUnit.Core`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:090-093.
