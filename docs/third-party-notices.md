# Third-Party Notices

Rig.TUnit redistributes or depends on the following open-source packages. License
names follow SPDX identifiers. This file is maintained manually; run
`dotnet list package --include-transitive` for an authoritative snapshot.

## Direct dependencies

### Test framework
- **TUnit** — MIT
- **TUnit.Assertions** — MIT
- **TUnit.Core** — MIT
- **Verify.TUnit** — MIT

### Container orchestration
- **Testcontainers for .NET** (all family modules) — MIT
  - `Testcontainers`, `.MsSql`, `.PostgreSql`, `.MySql`, `.Oracle`, `.Redis`,
    `.ServiceBus`, `.Kafka`, `.RabbitMq`, `.MongoDb`, `.CosmosDb`, `.Azurite`,
    `.LocalStack`, `.Minio`, `.Elasticsearch`, `.Cassandra`, `.KurrentDb`, `.Nats`

### Entity Framework Core
- **Microsoft.EntityFrameworkCore** (+ Design, Relational, Sqlite, SqlServer,
  InMemory) — Apache-2.0
- **Npgsql.EntityFrameworkCore.PostgreSQL** — PostgreSQL (BSD-style)
- **Pomelo.EntityFrameworkCore.MySql** — MIT
- **MySqlConnector** — MIT
- **Oracle.EntityFrameworkCore** — Oracle's OCP license (free)

### Mediator
- **Mediator.Abstractions** — MIT (Martin Othamar)
- **Mediator.SourceGenerator** — MIT

### gRPC + ASP.NET
- **Grpc.AspNetCore** (+ .Server, .Client) — Apache-2.0
- **Google.Protobuf** — BSD-3-Clause
- **Grpc.Tools** — Apache-2.0
- **Microsoft.AspNetCore.Mvc.Testing** — Apache-2.0
- **Microsoft.AspNetCore.Authentication.JwtBearer** — Apache-2.0

### Logging / Observability
- **Serilog** (+ .Extensions.Logging, .Sinks.Seq, .Sinks.Console, .Sinks.InMemory) — Apache-2.0
- **OpenTelemetry** (+ .Api, .Exporter.InMemory, .Extensions.Hosting,
  .Instrumentation.AspNetCore, .Instrumentation.Http) — Apache-2.0
- **Microsoft.ApplicationInsights** (+ .AspNetCore) — MIT

### Microsoft.Extensions.*
- All `Microsoft.Extensions.*` — MIT

### Security / Tokens
- **Microsoft.IdentityModel.Tokens** — MIT
- **System.IdentityModel.Tokens.Jwt** — MIT
- **Microsoft.IdentityModel.JsonWebTokens** — MIT

### Caching
- **StackExchange.Redis** — MIT
- **Microsoft.Extensions.Caching.StackExchangeRedis** — MIT
- **Microsoft.Extensions.Caching.Hybrid** — MIT
- **ZiggyCreatures.FusionCache** (+ .Backplane.StackExchangeRedis) — MIT

### Messaging
- **Azure.Messaging.ServiceBus** — MIT
- **Azure.Messaging.EventHubs** — MIT
- **Confluent.Kafka** — Apache-2.0
- **RabbitMQ.Client** — Apache-2.0 / MPL-2.0 (dual)
- **AWSSDK.SQS** — Apache-2.0
- **NATS.Client.Core** — Apache-2.0

### Storage
- **Azure.Storage.Blobs** — MIT
- **AWSSDK.S3** — Apache-2.0
- **Minio** — Apache-2.0
- **System.IO.Abstractions** — MIT

### NoSQL / Document / Search
- **Microsoft.Azure.Cosmos** — MIT
- **MongoDB.Driver** — Apache-2.0
- **AWSSDK.DynamoDBv2** — Apache-2.0
- **CassandraCSharpDriver** — Apache-2.0
- **Newtonsoft.Json** — MIT
- **KurrentDB.Client** — Apache-2.0
- **Elastic.Clients.Elasticsearch** — Elastic 2.0 / SSPL (dual)

### Resilience / Benchmarking / Analysis
- **Polly** (+ .Extensions) — BSD-3-Clause
- **BenchmarkDotNet** — MIT
- **NetArchTest.Rules** — MIT
- **Microsoft.CodeAnalysis.CSharp** (+ .Workspaces, .Analyzers, .Analyzer.Testing) — MIT
- **YamlDotNet** — MIT (Architecture rule tests)
- **Markdig** — BSD-2-Clause (Architecture rule tests)

### Data generation + mocking
- **Bogus** — MIT
- **NSubstitute** — BSD-3-Clause

### Coverage
- **coverlet.collector** — MIT
- **coverlet.msbuild** — MIT (deprecated for MTP-native path; see CONTRIBUTING.md)

## MIT's NOTICE requirement

MIT does not mandate a NOTICE file. This file exists for downstream due-diligence —
consumers embedding Rig.TUnit in larger commercial products can include this list in
their own third-party disclosures.

## Upstream license audit

License identifiers above are as-of 2026-04-20. For the live current state, run:

```sh
dotnet list package --include-transitive --format json > packages.json
```

Then inspect per-package `licenseUrl` entries.
