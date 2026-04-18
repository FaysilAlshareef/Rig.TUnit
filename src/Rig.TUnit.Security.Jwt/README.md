# Rig.TUnit.Security.Jwt

Fluent `JwtBuilder` producing tokens accepted by a real `JwtBearerHandler` — no
test-only authentication bypass. Supports HS256 / RS256 signing, kid rotation,
and negative builders (`BuildExpired`, `BuildNotYetValid`, `BuildTampered`).

## Install

```xml
<PackageReference Include="Rig.TUnit.Security.Jwt" />
```

## Example

```csharp
var token = JwtBuilder.Create(new JwtBuilderOptions {
                DefaultIssuer = "my-issuer",
                DefaultAudience = "my-audience" })
            .Subject("alice")
            .Claim("role", "admin")
            .ExpiresIn(TimeSpan.FromMinutes(15))
            .SignedWithHs256(keyBytes)
            .Build();

// Or negatives:
var expired = JwtBuilder.Create(opts).SignedWithHs256(key).BuildExpired(TimeSpan.FromMinutes(5));
var tampered = JwtBuilder.Create(opts).SignedWithHs256(key).BuildTampered();
```

## Dependencies
- `Rig.TUnit.Security`
- `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`

Spec: `003-rig-tunit-ecosystem-expansion` — FR:090, FR:093.
