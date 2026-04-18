# Rig.TUnit.Security.Mtls

Mutual-TLS testing fixture for Rig.TUnit. Generates a self-signed CA plus matched
client and server certificates in-memory — keys live only for the fixture lifetime
and are disposed deterministically.

## Install

```bash
dotnet add package Rig.TUnit.Security.Mtls
```

## Quick start

```csharp
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Security.Mtls.Builder;
using Rig.TUnit.Security.Mtls.Fixtures;

await using var fx = new MtlsFixture();
await fx.InitializeAsync();

MtlsAssert.BothSidesAuthenticated(fx.ClientCertificate, fx.ServerCertificate);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseMtls(RigConnect.FromValue(fx.Ca.Thumbprint), cfg => { })
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `CaSubject` | `CN=rigtunit-test-ca` | Subject DN of the self-signed CA |
| `ClientSubject` | `CN=rigtunit-client` | Subject DN of the client leaf cert |
| `ServerSubject` | `CN=rigtunit-server` | Subject DN of the server leaf cert |
| `ValidityDays` | `365` | CA + leaf validity window |

No production bypass — the generated chain validates via the real X.509 stack
and is suitable for Kestrel mTLS endpoints and HttpClient peer auth.
