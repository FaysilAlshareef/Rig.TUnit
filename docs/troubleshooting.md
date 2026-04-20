# Rig.TUnit Troubleshooting

Consolidated catalogue of common failure modes across providers. Leaf READMEs link
here from §10 via provider-specific anchors.

## Docker-related

### Tests hang on `await fixture.InitializeAsync()`
Testcontainers couldn't reach the Docker daemon. Verify:
1. `docker version` returns client + server info
2. On Windows, Docker Desktop has switched to Linux containers (not Windows)
3. The `DOCKER_HOST` env var matches your daemon socket

### Container image pulls fail on CI but work locally
GitHub Actions runners have ephemeral IPs — Docker Hub rate-limits anonymous pulls
to ~100/hr. Switch the pull to a mirror or add a Docker Hub login step.

## SQL providers

### Postgres `42P01: relation "X" does not exist` under parallel execution
Your tests share a physical database and a sibling test dropped the schema. Use
`PostgresDbContextHelper.CreateEphemeralDatabaseAsync` — every test gets its own DB.
See [Postgres README §8](../src/Rig.TUnit.Databases.Sql.Postgresql/README.md).

### SQL Server `Cannot drop database … because it is currently in use`
An EF `DbContext` retained a connection in the Npgsql/SqlClient pool. Call
`SqlConnection.ClearAllPools()` before the drop OR use
`pg_terminate_backend` / `KILL <spid>`.

### Oracle startup takes 60+ seconds
Oracle Free Edition does substantial bootstrap per container start. Bump
`StartupTimeoutSeconds` in `OracleFixtureOptions` OR run tests with
`[NotInParallel("oracle")]` to serialise them.

## NoSQL providers

### Mongo `A write operation resulted in an error` — race condition
Two tests used the same collection. Use
`Rig.TUnit.Databases.NoSql.Mongo.Helpers.CollectionPerTestHelper` — each test gets
a collection named `{prefix}_{IsolationKey}`.

### Cosmos Linux-emulator won't start on Windows runners
Cosmos emulator requires Linux containers; Windows runner's Docker uses the Windows
daemon by default. The CI workflow skips Cosmos on Windows via matrix `if:` guard.
For local development, switch Docker Desktop to Linux containers.

### Cassandra keyspace "cannot add duplicate column" under parallel tests
Two tests dropped/created the same keyspace concurrently. Use
`KeyspacePerTestHelper` — each test gets `ks_{IsolationKey}`.

## Messaging providers

### Kafka listener misses the first few messages
Kafka consumer joining a group takes ~1s for initial partition assignment. Wait for
the listener to be `Ready` before sending — `KafkaListener` exposes a `ReadyAsync`
signal.

### RabbitMQ "channel closed" under load
Connection pool exhausted. Increase `ChannelPool.MaxChannels` in options OR use a
`QueuePerTestHelper` so each test gets a dedicated queue + channel.

### Service Bus emulator startup flake
The Azure Service Bus emulator takes 20-60 s to become reachable. Ensure
`StartupTimeoutSeconds: 90` in `ServiceBusFixtureOptions`.

## Caching providers

### Redis SCAN returns duplicates
SCAN can return the same key across iterations. Use `KeyScanHelper`'s
`IAsyncEnumerable<string>` wrapper — it deduplicates.

### FusionCache backplane messages not received
The backplane requires a distinct `IConnectionMultiplexer` from the primary cache
or pub/sub messages bounce back. Configure per the ADR-007 split.

## Storage providers

### S3 "SignatureDoesNotMatch" under LocalStack
LocalStack S3 requires the `ForcePathStyle = true` client option — set in
`S3FixtureOptions.ClientConfiguration`.

### MinIO presigned URL expires mid-test
Default presigned expiry is 5 minutes; set `SasBuilder.ExpiryMinutes = 60` for
long-running snapshot flows.

## Security providers

### JWT validation fails with "IDX10501: Signature validation failed"
Kid rotation — the JWKS doesn't advertise the key used to sign. Test helpers should
use `MockOAuthServer`'s `CurrentKid` to match. See
[OAuth README §11](../src/Rig.TUnit.Security.OAuth/README.md).

### mTLS handshake fails with "The remote certificate is invalid"
The client presented the server certificate or vice versa. Verify
`MtlsCertificateBuilder` produced distinct client + server certs with matching chain.

## Observability providers

### Traces don't link parent → child spans
Check the W3C traceparent header is propagated via messaging / HTTP. The
`{Provider}Listener` + `{Provider}EventSender` helpers do this by default; direct
usage doesn't.

### AppInsights sink shows no data in the emulator
AppInsights requires a real ingestion endpoint — the emulator doesn't exist. Tests
that want AI-specific behaviour run against a real AI resource + a per-test
`InstrumentationKey` scoped to a test-only AppInsights app.

## Architecture-test failures

### `ProviderCompletenessTests: missing class {Provider}FixtureOptions`
A new provider was added without the canonical quartet. Copy the shape from an
existing leaf (e.g., `Rig.TUnit.Databases.Sql.SqlServer` — the reference provider).

### `NoSkipMarkersTests: contains [Category("SkipUntilFixed")]`
A skip marker was introduced outside the 4 legitimate architecture-rule files
(per FR-004). Either convert to per-test isolation OR delete the marker.

### `ReadmeCompletenessTests: section X missing`
A provider README drifted from the 14-section canonical template. See
[docs/templates/PROVIDER_README_TEMPLATE.md](templates/PROVIDER_README_TEMPLATE.md).
