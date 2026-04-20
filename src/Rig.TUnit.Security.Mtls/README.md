# Rig.TUnit.Security.Mtls

> Mutual-TLS fixture — ephemeral self-signed CA + matched client/server leaf certs for Kestrel mTLS tests. No production bypass.

## What this package is

An mTLS integration-test fixture. `MtlsFixture` generates a self-signed
CA plus matched client + server leaf certificates in-memory at fixture
startup; the keys live only for the fixture lifetime and dispose
deterministically. The generated chain validates via the real X.509
stack — suitable for Kestrel mTLS endpoints and `HttpClient` peer
authentication. No `ServerCertificateCustomValidationCallback` bypass.

## When to use it

- Integration tests for Kestrel endpoints configured with
  `RequireClientCertificate = true`.
- Verifying `CertificateAuthentication` middleware accepts / rejects
  chains correctly.
- Regression-testing certificate-rotation code paths.
- **Not for**: unit tests — mTLS requires real TCP + TLS negotiation.

## Prerequisites

- .NET 10 SDK
- Kestrel server under test configured for mTLS
- OpenSSL / .NET cert stack available (both are built-in on .NET 10).

## Quick start

```csharp
using Rig.TUnit.Security.Mtls.Fixtures;

await using var fx = new MtlsFixture();
await fx.InitializeAsync();

MtlsAssert.BothSidesAuthenticated(fx.ClientCertificate, fx.ServerCertificate);
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `CaSubject` | `string` | `"CN=rigtunit-test-ca"` | CA subject DN |
| `ClientSubject` | `string` | `"CN=rigtunit-client"` | Client leaf DN |
| `ServerSubject` | `string` | `"CN=rigtunit-server"` | Server leaf DN |
| `ValidityDays` | `int` | `365` | CA + leaf validity window |

## Fixture + helper APIs

- `Rig.TUnit.Security.Mtls.Fixtures.MtlsFixture`
- `Rig.TUnit.Security.Mtls.Options.MtlsFixtureOptions`
- `Rig.TUnit.Security.Mtls.Builder.MtlsRigBuilder`
- `Rig.TUnit.Security.Mtls.Assertions.MtlsAssert`

## Per-test isolation

Each `MtlsFixture` owns its own CA + leaf certs. Subjects include
`IsolationKey` when the test uses the default wiring, so parallel
tests produce distinct chains.

## Parallelism + performance

- Certificate generation: ~15–25 ms per fixture (CA + 2 leaves + RSA
  2048 keys).
- Memory-only — no disk writes, no registry.
- Parallelism: safe; each fixture is independent.

## Troubleshooting

- **`AuthenticationException` on handshake** — client cert not
  trusted. Ensure the Kestrel config's `ClientCertificateMode = RequireCertificate`
  AND the server trusts the fixture's CA (add via `fx.Ca`).
- **Cert expired immediately** — `ValidityDays` was set ≤ 0; default
  is 365, don't set it to 0.

See [docs/troubleshooting.md#mtls](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Keys are in-memory `X509Certificate2` with exportable private key;
  do not persist them (the fixture deliberately scopes keys to the
  fixture lifetime).
- `BothSidesAuthenticated` verifies both leaves chain to the fixture's
  CA; missing intermediates fail this check.
- The generated chain does NOT populate CRL/OCSP; revocation checks
  must be turned off for these chains.

## Benchmarks

See [`MtlsBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/MtlsBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`. Certificate generation is
the dominant cost and tracked closely.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Security`](../Rig.TUnit.Security/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
