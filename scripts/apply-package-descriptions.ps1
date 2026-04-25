#requires -Version 5.1
<#
    Inject <Description> into every src/**/*.csproj that lacks one.
    Idempotent - re-run safely. Only writes when content changed.

    Mapping table is the single source of truth (sourced from
    planning/release-readiness-and-nuget-publishing/NuGet-Package-Metadata-Audit.md).

    Run from repo root:  pwsh ./scripts/apply-package-descriptions.ps1
#>

[CmdletBinding()]
param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    if ($PSScriptRoot) {
        $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    } else {
        $RepoRoot = (Get-Location).Path
    }
}
Write-Host "RepoRoot: $RepoRoot"

$descriptions = @{
    'Rig.TUnit.Core' = 'Core abstractions for Rig.TUnit - RigBuilder, RigConnect, IsolationKey, fixture lifecycle, fluent assertions. Required by every other Rig.TUnit package.'
    'Rig.TUnit.Databases' = 'Family base for database fixtures - shared abstractions consumed by both SQL and NoSQL leaf packages.'
    'Rig.TUnit.Databases.Sql' = 'SQL family base - ISqlFixture, schema helpers, transaction-isolation utilities. Pull a leaf package (SqlServer, Postgresql, MySql, Oracle, Sqlite) for a working fixture.'
    'Rig.TUnit.Databases.Sql.SqlServer' = 'TUnit fixture for SQL Server backed by Testcontainers. Per-test isolation, schema bootstrapping, and assertions for SQL Server 2022+.'
    'Rig.TUnit.Databases.Sql.MySql' = 'TUnit fixture for MySQL backed by Testcontainers. Per-test isolation, schema bootstrapping, and assertions for MySQL 8.x.'
    'Rig.TUnit.Databases.Sql.Postgresql' = 'TUnit fixture for PostgreSQL backed by Testcontainers. Per-test isolation, schema bootstrapping, and assertions for Postgres 16.x.'
    'Rig.TUnit.Databases.Sql.Oracle' = 'TUnit fixture for Oracle Database (gvenzl/oracle-free) backed by Testcontainers. Per-test isolation and schema helpers for Oracle 23ai.'
    'Rig.TUnit.Databases.Sql.Sqlite' = 'TUnit fixture for SQLite - in-memory or file-based, no container required. The fastest tier of the SQL provider matrix.'
    'Rig.TUnit.Databases.NoSql' = 'NoSQL family base - INoSqlFixture, change-feed utilities, conflict-resolution helpers. Pull a leaf provider for a working fixture.'
    'Rig.TUnit.Databases.NoSql.Redis' = 'TUnit fixture for Redis-as-database backed by Testcontainers. Use for Redis-as-database scenarios; for caching see Rig.TUnit.Caching.Redis.'
    'Rig.TUnit.Databases.NoSql.Mongo' = 'TUnit fixture for MongoDB backed by Testcontainers. Per-test isolation, change-stream helpers, and BSON-aware assertions.'
    'Rig.TUnit.Databases.NoSql.Cosmos' = 'TUnit fixture for Azure Cosmos DB via the Linux emulator. Container-only; integration tests skip on Windows runners.'
    'Rig.TUnit.Databases.NoSql.Cassandra' = 'TUnit fixture for Apache Cassandra backed by Testcontainers. Per-test keyspace isolation and CQL assertion helpers.'
    'Rig.TUnit.Databases.NoSql.Dynamo' = 'TUnit fixture for Amazon DynamoDB via LocalStack. Per-test table provisioning and PartiQL-friendly assertions.'
    'Rig.TUnit.Databases.NoSql.ElasticSearch' = 'TUnit fixture for Elasticsearch backed by Testcontainers. Index lifecycle helpers and document-level assertions.'
    'Rig.TUnit.Databases.NoSql.KurrentDb' = 'TUnit fixture for KurrentDb (formerly EventStoreDB) backed by Testcontainers. Stream-aware lifecycle and read-model assertions.'
    'Rig.TUnit.Messaging' = 'Messaging family base - EventSenderBase, ListenerBase, unified SendContext (SessionKey, PartitionKey, DeduplicationKey), and ITopologyBuilder consumed by every messaging leaf package.'
    'Rig.TUnit.Messaging.ServiceBus' = 'TUnit fixture for Azure Service Bus backed by Testcontainers. Native sessions, deduplication, partition keys, and runtime topology builder for topics, subscriptions, and rules.'
    'Rig.TUnit.Messaging.Kafka' = 'TUnit fixture for Apache Kafka backed by Testcontainers. Per-key partition affinity, runtime topology builder, and per-partition offset snapshot utilities.'
    'Rig.TUnit.Messaging.RabbitMq' = 'TUnit fixture for RabbitMQ backed by Testcontainers. Routing keys, exchanges, bindings, and per-key ordering assertions.'
    'Rig.TUnit.Messaging.Nats' = 'TUnit fixture for NATS JetStream backed by Testcontainers. Stream lifecycle, ordered consumers, and x-session-key header propagation.'
    'Rig.TUnit.Messaging.Sqs' = 'TUnit fixture for Amazon SQS via LocalStack. FIFO queues, message groups, deduplication IDs, and DLQ binding helpers.'
    'Rig.TUnit.Caching' = 'Caching family base - ICacheFixture, stampede-protection helpers, tag-based invalidation utilities. Pull a leaf provider.'
    'Rig.TUnit.Caching.Redis' = 'TUnit fixture for Redis-as-cache backed by Testcontainers. Lock acquisition, tag invalidation, and TTL assertions.'
    'Rig.TUnit.Caching.Memory' = 'TUnit fixture for IMemoryCache - in-process, no container required. Suitable for unit-style cache tests.'
    'Rig.TUnit.Caching.Hybrid' = 'TUnit fixture for ASP.NET Core HybridCache (L1+L2). Layered cache assertions and stampede-prevention checks.'
    'Rig.TUnit.Caching.Fusion' = 'TUnit fixture for ZiggyCreatures FusionCache. Layered cache, fail-safe, and adaptive caching test helpers.'
    'Rig.TUnit.Storage' = 'Storage family base - IBlobFixture, multipart helpers, conditional-request assertions. Pull a leaf provider.'
    'Rig.TUnit.Storage.AzureBlob' = 'TUnit fixture for Azure Blob Storage via Azurite. Per-test container isolation, multipart upload, and condition-header assertions.'
    'Rig.TUnit.Storage.FileSystem' = 'TUnit fixture for the local file system - temp-folder-scoped, no container required.'
    'Rig.TUnit.Storage.MinIO' = 'TUnit fixture for MinIO (S3-compatible) backed by Testcontainers. Per-test bucket isolation and lifecycle policy helpers.'
    'Rig.TUnit.Storage.S3' = 'TUnit fixture for Amazon S3 via LocalStack. Per-test bucket isolation, multipart upload, and SSE/replication assertions.'
    'Rig.TUnit.Observability' = 'Observability family base - TelemetryFixtureBase, exemplar helpers, log-redaction utilities. Pull a leaf provider.'
    'Rig.TUnit.Observability.Logging' = 'TUnit fixture for ILogger-based assertions - captured-log buffer, level filters, scope-aware queries.'
    'Rig.TUnit.Observability.Logging.Analyzers' = 'Roslyn analyzers for log-template hygiene used by tests in this rig.'
    'Rig.TUnit.Observability.Metrics' = 'TUnit fixture for System.Diagnostics.Metrics - meter/instrument capture, histogram-bucket assertions.'
    'Rig.TUnit.Observability.Tracing' = 'TUnit fixture for OpenTelemetry traces - Activity capture, parent-child propagation, and cross-boundary correlation assertions.'
    'Rig.TUnit.Observability.Seq' = 'TUnit fixture for Seq backed by Testcontainers. End-to-end log pipeline assertions against a real Seq instance.'
    'Rig.TUnit.Observability.AppInsights' = 'TUnit fixture for Azure Application Insights - telemetry capture via the offline channel.'
    'Rig.TUnit.Security' = 'Security family base - IIdentityFixture, claim helpers, and shared crypto utilities consumed by every security leaf package.'
    'Rig.TUnit.Security.Jwt' = 'TUnit fixture for issuing test JWTs and a JWKS endpoint backed by an in-memory signing key.'
    'Rig.TUnit.Security.OAuth' = 'TUnit fixture for OAuth flows (auth-code+PKCE, client-credentials) against an in-process IdentityServer or stub provider.'
    'Rig.TUnit.Security.Mtls' = 'TUnit fixture for mutual-TLS scenarios - test CAs, leaf certs, and revocation list assertions.'
    'Rig.TUnit.Security.Policies' = 'TUnit fixture for ASP.NET Core authorisation policies - claim-based, role-based, and resource-based assertions.'
    'Rig.TUnit.Microservices' = 'Microservices family base - patterns and contracts shared across EventSourcing, Outbox, Inbox, Saga, Snapshots, Contracts.'
    'Rig.TUnit.Microservices.EventSourcing' = 'TUnit fixture for event-sourced aggregates - given/when/then helpers, stream replay, and projection-drift assertions.'
    'Rig.TUnit.Microservices.Outbox' = 'TUnit fixture for the transactional outbox pattern - visibility-timeout helpers, dispatcher assertions, and dedup checks.'
    'Rig.TUnit.Microservices.Inbox' = 'TUnit fixture for inbox-pattern receivers - idempotent handler assertions and replay helpers.'
    'Rig.TUnit.Microservices.Saga' = 'TUnit fixture for saga workflows - timeout simulation, compensation tracking, and step-by-step assertions.'
    'Rig.TUnit.Microservices.Snapshots' = 'TUnit fixture for snapshot-and-restore - point-in-time capture and projection rebuild helpers.'
    'Rig.TUnit.Microservices.Contracts' = 'TUnit fixture for consumer-driven contract tests - Pact-friendly helpers and contract-drift assertions.'
    'Rig.TUnit.Http' = 'TUnit fixture for HTTP integration testing - captured request/response model, redirect/cookie/CORS assertions, and protocol-aware (HTTP/1.1, HTTP/2, HTTP/3) helpers.'
    'Rig.TUnit.Grpc' = 'TUnit fixture for gRPC clients and servers - channel reconnection, deadline assertion, streaming helpers.'
    'Rig.TUnit.HealthChecks' = 'TUnit fixture for ASP.NET Core health-check assertions - liveness/readiness lifecycle and degraded-state simulation.'
    'Rig.TUnit.Resilience' = 'TUnit fixture for Polly v8 resilience pipelines - composite-policy assertions, circuit-state simulation, and chaos-injection helpers.'
    'Rig.TUnit.Mediator' = 'TUnit fixture for MediatR / generic mediator pipelines - handler invocation capture and pipeline-behaviour assertions.'
    'Rig.TUnit.Docker' = 'Low-level Docker control surface used by container-backed fixtures - image pulls, network shaping, and lifecycle helpers.'
    'Rig.TUnit.Parallelism' = 'Parallelism control helpers - per-test isolation keys, shared-fixture rationale enforcement, and concurrency-fuzzing utilities.'
    'Rig.TUnit.Concurrency' = 'Concurrency primitives for tests - async-context propagation, deterministic interleaving, and shuffle-replay helpers.'
    'Rig.TUnit.Ci' = 'CI integration helpers - runner detection, artefact-path normalisation, and step-summary writers.'
    'Rig.TUnit.WebAPI' = 'TUnit fixture for ASP.NET Core Web APIs - WebApplicationFactory-style host, OpenAPI drift assertions, and per-test client isolation.'
}

$srcDir = Join-Path $RepoRoot 'src'
$processed = 0
$skipped   = 0
$updated   = 0
$missing   = New-Object System.Collections.Generic.List[string]

Get-ChildItem -Path $srcDir -Recurse -Filter '*.csproj' | ForEach-Object {
    $processed++
    $path = $_.FullName
    $packageId = $_.BaseName
    $raw = [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))

    if ($raw -match '<Description>') {
        $skipped++
        return
    }

    if (-not $descriptions.ContainsKey($packageId)) {
        $missing.Add($packageId)
        return
    }

    $desc = $descriptions[$packageId]
    # XML-encode the description (basic - only & < >).
    $descEncoded = $desc.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')

    if ($raw -match '<PropertyGroup>\s*</PropertyGroup>') {
        $newContent = $raw -replace '<PropertyGroup>\s*</PropertyGroup>', "<PropertyGroup>`r`n    <Description>$descEncoded</Description>`r`n  </PropertyGroup>"
    }
    elseif ($raw -match '(?ms)<PropertyGroup>(.*?)</PropertyGroup>') {
        # Append <Description> as the first child of the first non-empty PropertyGroup.
        $rx = [System.Text.RegularExpressions.Regex]::new('<PropertyGroup>')
        $newContent = $rx.Replace(
            $raw,
            "<PropertyGroup>`r`n    <Description>$descEncoded</Description>",
            1)  # Only the first match.
    }
    else {
        # No PropertyGroup at all - inject one right after the opening <Project ... > tag.
        $rx = [System.Text.RegularExpressions.Regex]::new('(<Project[^>]*>)')
        $newContent = $rx.Replace(
            $raw,
            "`$1`r`n  <PropertyGroup>`r`n    <Description>$descEncoded</Description>`r`n  </PropertyGroup>",
            1)
    }

    if ($newContent -ne $raw) {
        # Use UTF-8 without BOM (csproj convention) and explicit byte writes to avoid
        # Windows PowerShell 5.1's default cp1252 conversion.
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($path, $newContent, $utf8NoBom)
        $updated++
        Write-Host "  updated: $packageId" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Processed: $processed"
Write-Host "Already had <Description>: $skipped"
Write-Host "Updated: $updated"
if ($missing.Count -gt 0) {
    Write-Host "MISSING from mapping (no description applied):" -ForegroundColor Yellow
    $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    exit 1
}
exit 0
