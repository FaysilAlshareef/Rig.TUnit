# Rig.TUnit.Databases.NoSql.ElasticSearch

> Testcontainers-backed Elasticsearch 8.x fixture with `IndexRefreshHelper` and strongly-typed `DslAssert`.

## What this package is

The Rig.TUnit Elasticsearch provider. `ElasticSearchFixture` spins
Elasticsearch 8.x via Testcontainers and returns an `ElasticsearchClient`
configured for HTTPS + basic auth against the container's self-signed
certificate. `IndexRefreshHelper.RefreshAsync` forces the near-real-time
refresh so indexed documents are queryable immediately — required because
Elasticsearch's default 1-second refresh interval would otherwise race
every test. `DslAssert.HitCountAsync<T>` provides a fluent hit-count
assertion over typed DSL queries.

## When to use it

- Integration tests for full-text search features.
- Asserting indexed document counts or query-DSL equivalence.
- Verifying index template / mapping behaviour.
- **Not for**: pure-unit search-logic tests — Elasticsearch's query DSL
  is best tested against a real server.

## Prerequisites

- .NET 10 SDK
- Docker Desktop / Colima (Elasticsearch image ~1.1 GB)
- `Elastic.Clients.Elasticsearch` 8.x (transitive; note Elastic 2.0 / SSPL
  dual licence).

## Quick start

```csharp
using Rig.TUnit.Databases.NoSql.ElasticSearch.Fixtures;
using Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers;

await using var fx = new ElasticSearchFixture();
await fx.InitializeAsync();

await fx.Client.Indices.CreateAsync("orders");
await IndexRefreshHelper.RefreshAsync(fx.Client, "orders");
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `Image` | `string` | `"docker.elastic.co/elasticsearch/elasticsearch:8.15.0"` | Image |
| `StartupTimeoutSeconds` | `int` | `180` | ES boot is slow |
| `Password` | `string` | `"rigtunit"` | `elastic` user password |
| `DiscoveryType` | `string` | `"single-node"` | Dev mode |
| `XpackSecurityEnabled` | `bool` | `true` | Auth enforced |

## Fixture + helper APIs

- `Rig.TUnit.Databases.NoSql.ElasticSearch.Fixtures.ElasticSearchFixture`
- `Rig.TUnit.Databases.NoSql.ElasticSearch.Options.ElasticSearchFixtureOptions`
- `Rig.TUnit.Databases.NoSql.ElasticSearch.Builder.ElasticSearchRigBuilder`
- `Rig.TUnit.Databases.NoSql.ElasticSearch.Helpers.IndexRefreshHelper`
- `Rig.TUnit.Databases.NoSql.ElasticSearch.Assertions.DslAssert`

## Per-test isolation

Per-test index naming via `IsolationKey`: `orders_{IsolationKey:short}`.
`DELETE {index}` on teardown. Indexes are cheap; full parallelism is safe.

## Parallelism + performance

- First-run pull: ~90 s (~1.1 GB).
- Warm startup: ~45–60 s.
- Per-test index create + delete: ~50 ms.
- Parallelism: 8+ concurrent tests; ES handles index churn well.

## Troubleshooting

- **`search_phase_execution_exception` with zero hits immediately after
  indexing** — you forgot `IndexRefreshHelper.RefreshAsync`. Default
  refresh interval is 1 s; tests cannot wait.
- **Certificate validation errors** — the fixture disables cert
  validation on the embedded `HttpClient`; do not share that handler
  with production-shaped code.

See [docs/troubleshooting.md#elasticsearch](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- Elasticsearch's `match` query is analysed by default; use `term` to hit
  exact tokens. Tests asserting on non-tokenised content must say so.
- `_id` is not searchable by default; use `ids` query or a mapped `keyword`
  field.
- Index settings are immutable after creation for many fields (shard count,
  analyser); tests that mutate these must recreate the index.

## Benchmarks

See [`ElasticSearchBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/ElasticSearchBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- Family base: [`Rig.TUnit.Databases.NoSql`](../Rig.TUnit.Databases.NoSql/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
