# Release Notes — `v0.1.0-beta.1`

**Tag**: `v0.1.0-beta.1`
**NuGet**: [`Rig.TUnit` 0.1.0-beta.1](https://www.nuget.org/packages/Rig.TUnit/0.1.0-beta.1)
**Status**: First public preview — APIs may change before `1.0.0`.

> This is the body of the GitHub Release. When cutting the tag, copy this file's content
> (everything below the front-matter) into the GitHub Release description.

---

## Highlights

**Rig.TUnit is a TUnit-first integration-testing rig for .NET** — fixtures, builders, and
assertions for SQL, NoSQL, messaging, caching, storage, observability, security, and
microservices. This first preview ships **70 NuGet packages** covering 9 provider families.

```bash
dotnet add package Rig.TUnit --prerelease
dotnet add package Rig.TUnit.Databases.Sql.Postgresql --prerelease
```

```csharp
[Test]
public async Task Repository_Persists(CancellationToken ct)
{
    await using var db = new PostgresFixture();
    await db.InitializeAsync();

    await using var conn = new NpgsqlConnection(db.ConnectionString);
    await conn.ExecuteAsync("INSERT INTO orders (id) VALUES (gen_random_uuid())");

    var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM orders");
    await Assert.That(count).IsEqualTo(1);
}
```

---

## What's in the box

### Core (1 package)
- `Rig.TUnit.Core` — base abstractions: `RigBuilder`, `RigConnect`, `IsolationKey`, fixture
  lifecycle, fluent assertion entry points.

### Convenience meta-packages (2 packages)
- `Rig.TUnit` — Core + Mediator + Grpc + WebAPI (default entry point).
- `Rig.TUnit.All` — every provider package (discouraged; prefer per-feature meta).

### SQL databases (6 packages)
- `Rig.TUnit.Databases.Sql` (family base) — `Rig.TUnit.Databases.Sql.SqlServer`,
  `…MySql`, `…Postgresql`, `…Oracle`, `…Sqlite`.

### NoSQL databases (8 packages)
- `Rig.TUnit.Databases.NoSql` (family base) — `…Redis`, `…Mongo`, `…Cosmos`, `…Cassandra`,
  `…Dynamo`, `…ElasticSearch`, `…KurrentDb`.

### Messaging (6 packages, full Feature-007 parity)
- `Rig.TUnit.Messaging` (family base) — `…ServiceBus`, `…Kafka`, `…RabbitMq`, `…Nats`, `…Sqs`.
- Unified `SendContext` (`SessionKey`, `PartitionKey`, `DeduplicationKey`) across providers.
- Provider-specific `WithTopology(...)` builders for runtime topic / queue / stream creation.
- Per-key ordering assertions via `OrderingAssert.PerKeyMonotonic`.

### Caching (5 packages)
- `Rig.TUnit.Caching` — `…Redis`, `…Memory`, `…Hybrid`, `…Fusion`.

### Storage (5 packages)
- `Rig.TUnit.Storage` — `…AzureBlob`, `…FileSystem`, `…MinIO`, `…S3`.

### Observability (6 packages)
- `Rig.TUnit.Observability` — `…Logging`, `…Metrics`, `…Tracing`, `…Seq`, `…AppInsights`,
  `Rig.TUnit.Observability.Logging.Analyzers`.

### Security (5 packages)
- `Rig.TUnit.Security` — `…Jwt`, `…OAuth`, `…Mtls`, `…Policies`.

### Microservices (7 packages)
- `Rig.TUnit.Microservices` — `…EventSourcing`, `…Outbox`, `…Inbox`, `…Saga`, `…Snapshots`,
  `…Contracts`.

### Infrastructure (7 packages)
- `Rig.TUnit.Http`, `…Grpc`, `…HealthChecks`, `…Resilience`, `…Mediator`, `…Docker`,
  `…Parallelism`, `…Concurrency`, `…Ci`, `…WebAPI`.

---

## Engineering quality

- **Target framework**: `net10.0`.
- **Coverage gate**: line ≥ 0.90, branch ≥ 0.85 — enforced in CI.
- **Architecture tests**: provider completeness, dependency direction, no skip markers,
  shared-fixture rationale, no leaky transitive deps — all enforced on every PR.
- **Source Link**: every package ships with embedded sources; debugger steps into Rig.TUnit
  source from a consumer project without separate setup.
- **Symbols**: `.snupkg` published alongside every `.nupkg` to nuget.org.
- **Deterministic builds**: `ContinuousIntegrationBuild=true`, `Deterministic=true`,
  `EmbedUntrackedSources=true` — builds reproducible from a tag.
- **Trusted Publishing**: published to nuget.org via OIDC — no long-lived API keys.
- **Code of Conduct**: Contributor Covenant 2.1.

---

## Install

The most common starting point:

```bash
dotnet add package Rig.TUnit --prerelease
```

Then add one or more provider packages:

```bash
# SQL
dotnet add package Rig.TUnit.Databases.Sql.Postgresql --prerelease

# Messaging
dotnet add package Rig.TUnit.Messaging.ServiceBus --prerelease

# Caching
dotnet add package Rig.TUnit.Caching.Redis --prerelease

# Microservices
dotnet add package Rig.TUnit.Microservices.Outbox --prerelease
```

Wire into your test setup:

```csharp
services.AddRigTUnit(rig =>
    rig.UsePostgres(RigConnect.FromValue(cs), pg =>
        pg.ReplaceDbContext<AppDbContext>())
       .UseServiceBus(RigConnect.FromValue(sbCs), _ => { }));
```

---

## Known limitations & beta caveats

- **API may change before 1.0.** This is a preview release. Breaking changes between
  betas are signalled in `CHANGELOG.md`; review before upgrading. From the next minor onward,
  `<EnablePackageValidation>` enforces semver compatibility automatically.
- **Cosmos emulator on Windows runners**: integration tests require Linux containers and are
  skipped on Windows CI; local devs on Windows hit the same limit.
- **Oracle Free image is large** (~2.3 GB) — first run pulls the image; subsequent runs hit
  the docker layer cache.
- **`Rig.TUnit.All` is discouraged** — prefer the per-stack metas (`Rig.TUnit`,
  `Rig.TUnit.Microservices`). It exists for prototyping convenience only.

---

## What's next (roadmap excerpts)

- **Feature 008** — additional providers: Pulsar, MQTT, Google Pub/Sub, Memcached.
- **Feature 009** — fluent builder expansion: assertion DSL for cross-provider invariants
  (e.g. "outbox row → Service Bus topic → query-side projection landed within X ms").
- **Feature 010** — deterministic clock for time-sensitive tests (saga timeouts, cache TTL,
  outbox visibility timeout).
- See `planning/` for the full pipeline of ~50 planned briefs.

---

## Documentation

- Repository: <https://github.com/FaysilAlshareef/Rig.TUnit>
- Quick-start: [`README.md`](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/README.md)
- Contributing: [`CONTRIBUTING.md`](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/CONTRIBUTING.md)
- Code of Conduct: [`CODE_OF_CONDUCT.md`](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/CODE_OF_CONDUCT.md)
- Security policy: [`SECURITY.md`](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/SECURITY.md)
- Changelog: [`CHANGELOG.md`](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/CHANGELOG.md)
- Discussions: <https://github.com/FaysilAlshareef/Rig.TUnit/discussions>

---

## Reporting issues

- **Bugs**: [open a bug report](https://github.com/FaysilAlshareef/Rig.TUnit/issues/new?template=bug_report.yml)
- **Feature requests**: [open a feature request](https://github.com/FaysilAlshareef/Rig.TUnit/issues/new?template=feature_request.yml)
- **Provider request**: [open a provider request](https://github.com/FaysilAlshareef/Rig.TUnit/issues/new?template=provider_request.yml)
- **Security**: see [`SECURITY.md`](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/SECURITY.md) — do **not** open a public issue.

---

## Acknowledgements

Rig.TUnit stands on the shoulders of:

- [TUnit](https://tunit.dev) — the modern .NET test framework this rig is built around.
- [Testcontainers for .NET](https://dotnet.testcontainers.org) — every container fixture wraps
  a Testcontainers module.
- [Bogus](https://github.com/bchavez/Bogus), [Verify](https://github.com/VerifyTests/Verify),
  [Microsoft.Extensions.\*](https://github.com/dotnet/runtime), and the broader .NET OSS
  ecosystem.

Thank you for trying the preview — feedback in [Discussions](https://github.com/FaysilAlshareef/Rig.TUnit/discussions)
is the fastest path to influence the `1.0` API.

---

**License**: [MIT](https://github.com/FaysilAlshareef/Rig.TUnit/blob/master/LICENSE).
