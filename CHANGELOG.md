# Changelog

All notable changes to Rig.TUnit are documented in this file. Format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versioning follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-beta.2] — 2026-04-27 — Packaging + benchmark methodology fixes

Two fixes uncovered while validating the v0.1.0-beta.1 release:

### Fixed

- **Per-package README on nuget.org** (#19). All 63 published `Rig.TUnit.*` packages
  in v0.1.0-beta.1 rendered the same generic landing copy because
  `Directory.Build.props` packed the repo-level `docs/nuget/README.md` into every
  `.nupkg`. Each `src/Rig.TUnit.*/README.md` is package-specific (Quick start,
  install, API), but the build wasn't using them. Switched the `<None Include>`
  source to `$(MSBuildProjectDirectory)\README.md` with an `Exists()` guard;
  the repo-level README remains as a defensive fallback for any future project
  that doesn't carry one. From v0.1.0-beta.2 onwards, each NuGet listing renders
  its own README. v0.1.0-beta.1 packages on nuget.org cannot be patched —
  `--skip-duplicate` will leave them as-is and the corrected content lands on
  the new version.
- **Benchmark workflow false-positive alerts** (#20). The 2026-04-26 master
  benchmark run alerted on ~106 of 182 benchmarks under the 120 % threshold,
  including ~95 in code paths #18 never touched. Cross-run analysis traced this
  to two layered methodology bugs:
  - `--job short` (1 warmup × 3 iterations × 1 launch) on shared GitHub-hosted
    runners produces ±50 % per-bench variance run-to-run on identical source
    code. The "previous baseline" run (`1311139`, a Bump-checkout-only commit)
    measured 26 % faster than its own predecessor — that anomaly was what the
    current run was being compared against.
  - `InProcessEmitBenchmarkConfig` (used by the 21 provider benchmark classes
    via `[Config(typeof(...))]`) was wired with `Job.Dry` + `RunStrategy.ColdStart`
    + `IterationCount = 1` + `InvocationCount = 1` — a single-invocation cold-start
    smoke shape that included JIT compile time in the measurement. That's why
    `MySql.Options_ConstructWithDefaults` was reporting 0.5–1 ms per
    `new Options()` call.

  Two-commit fix:
  - New global `CiBenchmarkConfig` (3 warmup × 5 iterations × 1 launch,
    Throughput strategy). Workflow no longer passes `--job` on the CLI.
    `auto-push` and `fail-on-alert` gated on `github.ref == refs/heads/master`,
    so workflow_dispatch runs from feature branches don't pollute master's
    gh-pages baseline timeline.
  - `InProcessEmitBenchmarkConfig` switched to the same 3 × 5 × 1 shape;
    `InProcessEmitToolchain` retained because BDN's external auto-generated
    boilerplate build still exceeds the 2-minute timeout on the 100+ project
    transitive graph.

  Validation across 4 dispatched runs on `chore/benchmark-regression-rca`:
  external-suite between-run median ratio is now 1.036 (was diverging to 1.30
  under `--job short`); 1.7 % of external benchmarks drift > 1.20× run-to-run
  on identical source. The remaining ~9 % residual noise concentrates in
  sub-100 ns `Options_ConstructWith*` benchmarks where measurement floor on
  shared CPU exceeds the alert threshold by definition; a follow-up will drop
  or `[BenchmarkCategory]`-skip those.

## [0.1.0-beta.1] — 2026-04-26 — First public release on nuget.org

This is the inaugural public publish of Rig.TUnit on nuget.org. It bundles the
internal 0.1.0–0.4.0 milestone work documented below plus the release-readiness
layer and post-#10 CI hygiene into one coherent first cut. Subsequent stable
release will be `v0.1.0`; minor bumps (`v0.2.0`, …) follow from there.

### Changed — Post-#10 CI hygiene & gh-pages 404 fix

- `actions/upload-artifact` bumped 4 → 7 across `ci.yml`, `release.yml`,
  `benchmark.yml` (15 call-sites). v4 was deprecated on Node 20; v7 ships on
  Node 24. Matrix shards already produced per-shard artifact names, so v4's
  immutable-name rule was satisfied — no producer/consumer renames.
- `github/codeql-action` (init + analyze) bumped 3 → 4 in `codeql.yml`.
- `codeql.yml` switched the C# extractor to `build-mode: none`. Previously the
  CodeQL run spent ~12 of every 15 minutes on `dotnet restore + build` of
  `Rig.TUnit.slnx` (60+ projects). With no-build extraction the run lands in
  ~3–5 min and keeps full `security-and-quality` coverage. `setup-dotnet` and
  the explicit Restore + Build step are dropped from the workflow.
- `benchmark.yml` gained an `Ensure gh-pages root redirect` post-deploy step.
  `benchmark-action/github-action-benchmark` publishes the dashboard under
  `gh-pages/dev/bench/`, so the project Pages root previously 404'd. The new
  step writes a one-line `<meta http-equiv="refresh">` redirect at gh-pages
  root once and is a no-op on subsequent runs.
- `tests/Rig.TUnit.Architecture.Tests/Rules/{ArtifactUploadTests,
  CoverageSummaryJobTests}.cs` — pinned upload-artifact prefix updated 4 → 7
  to match the new floor.

### Fixed — Post-#10 ServiceBus integration flake fixes

- `PartitionedFanoutTests.SendAsync_MessagesWithDistinctPartitionKeys_AllReachSubscription`
  — the wait loop now polls per-partition-key distinct-body counts instead of
  total `Captured.Count`. Service Bus can redeliver a message whose peek-lock
  expires before completion, so total count would climb from duplicates while
  one slow partition still had unique arrivals pending. The test then failed
  with "Expected ≥ 4 but received 1" while other partitions had inflated
  shares. Per-key distinct-count also guards the final assertion against
  redelivery duplicates.
- `DlqRedriveTests.SendAsync_MessageAbandonedPastMaxDeliveryCount_AppearsOnDeadLetterQueue`
  — abandon counter tracks abandons of the test message specifically, not
  loop iterations. The subscription is created on the shared `test-topic`
  used by other parallel tests, so messages from those tests fan out to it
  too. The previous loop burned its iterations completing foreign messages
  and rarely reached `MaxDeliveryCount=3` for the test message; the broker
  never auto-DLQ'd and the probe timed out at 90s.
- `ServiceBusDeadLetterProbe.HasMessageAsync` — the timeout is now caller-
  controlled with the linked CTS distinguishing inner-deadline elapse from
  caller cancel; the diagnostic on timeout no longer leaks an
  `OperationCanceledException` when the caller's token is still healthy.

### Added — Release readiness & open-source onboarding

- **NuGet publishing pipeline** (`.github/workflows/release.yml`) - tag-driven, OIDC-trusted
  publishing to nuget.org with a protected `nuget-org` GitHub environment for owner approval;
  GitHub Packages mirror; deterministic builds; embedded sources; symbol packages (`.snupkg`).
- **Versioning** - MinVer drives `<Version>` from the latest `v*` git tag; untagged builds
  produce `0.0.0-alpha.0.{height}.{sha}`.
- **Source Link** - `Microsoft.SourceLink.GitHub` enables step-into debugging for consumers.
- **Per-package metadata** - every `src/**` package now ships with `<Description>`, repo URL,
  project URL, MIT license expression, NuGet README, tags. Centralised in
  `Directory.Build.props` + `src/Directory.Build.props`; per-project descriptions applied via
  `scripts/apply-package-descriptions.ps1` (idempotent).
- **Community files** - `CODE_OF_CONDUCT.md` (Contributor Covenant 2.1, fetched via
  `scripts/install-coc.sh`), `.github/CODEOWNERS`, `.github/PULL_REQUEST_TEMPLATE.md`,
  `.github/ISSUE_TEMPLATE/{bug,feature,provider,docs}.yml`,
  `.github/DISCUSSION_TEMPLATE/{q-and-a,show-and-tell,ideas}.yml`, `.github/FUNDING.yml`,
  `.github/dependabot.yml` (weekly NuGet grouped + GitHub Actions).
- **Security workflows** - CodeQL C# weekly + on-PR (`.github/workflows/codeql.yml`);
  release-drafter automation (`.github/workflows/release-drafter.yml` +
  `.github/release-drafter.yml`); stale-issue/PR bot (`.github/workflows/stale.yml`).
- **CI improvements** - workflow-scoped concurrency (cancel-in-progress on PR refs); reusable
  `.github/actions/setup-dotnet-cache` composite action with NuGet cache keyed on
  `Directory.Packages.props` + `**/*.csproj`; new `pack-validate` job that builds every
  packable project and fails the PR when description / authors / projectUrl / repository /
  readme / license metadata is missing.
- **`docs/RELEASING.md`** - the full release ritual (changelog update, tag push, owner
  approval, post-publish verification, recovery procedures).
- **Hand-off scripts** under `scripts/` - `apply-repo-settings.sh`,
  `apply-branch-protection.sh`, `setup-nuget-environment.sh` - all idempotent, run once
  during release-readiness rollout.

### Changed

- `Directory.Build.props` - added shared NuGet packaging properties (Authors, RepositoryUrl,
  PackageProjectUrl, PackageLicenseExpression, PackageReadmeFile, PackageIcon, PackageTags,
  IncludeSymbols, SymbolPackageFormat, EmbedUntrackedSources, ContinuousIntegrationBuild,
  Deterministic, EnablePackageValidation). Default `IsPackable=false`.
- `src/Directory.Build.props` (new) - flips `IsPackable=true` so every production project
  packs by default.
- `tests/Directory.Build.props` (new) - explicit `IsPackable=false` defence-in-depth so test
  and benchmark projects can never be published as packages.
- `Directory.Packages.props` - added MinVer 6.0.0 + Microsoft.SourceLink.GitHub 8.0.0
  central versions.
- `src/Rig.TUnit.Observability.Logging.Analyzers` - marked `IsPackable=false`. The analyzer
  is internal log-template hygiene used by this rig's own tests; it is not a consumer-facing
  package. Re-enable when it ships consumer-facing rules.

### Removed

- Duplicate markdown link checker (`gaurav-nelson/github-action-markdown-link-check`) -
  consolidated under the existing lychee `linkcheck` job.
- `red-commit-verification` job - was emitting only `::notice::` log lines without
  performing any real verification. `commit-discipline-gate` continues to enforce RED -> GREEN
  pairing on every PR.

### Security

- `secret_scanning` and `secret_scanning_push_protection` enabled (applied via
  `scripts/apply-repo-settings.sh`).
- Trusted Publishing to nuget.org via OIDC - no long-lived API keys in repository secrets.
- Branch protection ruleset applied to `master`: 1 CODEOWNERS approval, signed required
  status checks, linear history, no force pushes, no deletion. Tag protection on `v*` so
  only admins can publish releases.

### Public package surface (initial drop)

62 packages across 9 families (analyzer is internal). Quick inventory:

**Core**: `Rig.TUnit.Core`.
**Meta-packages**: `Rig.TUnit`, `Rig.TUnit.All`.
**SQL**: `Rig.TUnit.Databases.Sql{,.SqlServer,.MySql,.Postgresql,.Oracle,.Sqlite}`.
**NoSQL**: `Rig.TUnit.Databases.NoSql{,.Redis,.Mongo,.Cosmos,.Cassandra,.Dynamo,.ElasticSearch,.KurrentDb}`.
**Messaging**: `Rig.TUnit.Messaging{,.ServiceBus,.Kafka,.RabbitMq,.Nats,.Sqs}`.
**Caching**: `Rig.TUnit.Caching{,.Redis,.Memory,.Hybrid,.Fusion}`.
**Storage**: `Rig.TUnit.Storage{,.AzureBlob,.FileSystem,.MinIO,.S3}`.
**Observability**: `Rig.TUnit.Observability{,.Logging,.Metrics,.Tracing,.Seq,.AppInsights}`.
**Security**: `Rig.TUnit.Security{,.Jwt,.OAuth,.Mtls,.Policies}`.
**Microservices**: `Rig.TUnit.Microservices{,.EventSourcing,.Outbox,.Inbox,.Saga,.Snapshots,.Contracts}`.
**Infrastructure**: `Rig.TUnit.{Http,Grpc,HealthChecks,Resilience,Mediator,Docker,Parallelism,Concurrency,Ci,WebAPI}`.

### Added — Feature 007 Phases 0+1+2+3 (planned release N+1: SendContext · ServiceBus · Kafka · SQS)

- `SendContext` record — unified `SessionKey`, `PartitionKey`, `DeduplicationKey` fields shared
  by all messaging providers; `BuildHeaders(SendContext, …)` overload on `EventSenderBase`.
  `CapturedMessage<TMessage>.Body` narrowed from `string?` → `string`; `SessionKey` added
  to `CapturedMessage<TMessage>` (FR-007-01 — T000).
- `ITopologyBuilder` marker interface; `ProviderCompletenessTests` parity gate +
  `.parity-coverage.txt` automation (FR-007-03 — T001/T002/T003).
- **Azure Service Bus**: `ServiceBusEventSender.SendAsync(SendContext)` overload sets session
  ID, `MessageId` (deduplication), and partition key on the outgoing `ServiceBusMessage`
  (T010). `ServiceBusSessionListener` wraps the native `ServiceBusSessionProcessor` and
  surfaces `CapturedMessage.SessionKey` from the session ID (T011). `IServiceBusTopologyBuilder`
  + `ServiceBusTopologyBuilder` (topic + subscription + rule + max-size configuration);
  `ServiceBusRigBuilder.WithTopology` hook (T012/T013).
- **Apache Kafka**: `KafkaEventSender.SendAsync(SendContext)` sets `Message.Key` from
  `PartitionKey ?? SessionKey` for per-key partition affinity (T020/T021). `IKafkaTopologyBuilder`
  + `KafkaTopologyBuilder` (topic + partition + replication-factor); `KafkaRigBuilder.WithTopology`
  hook (T022/T023). `TopicPartitionOffsetSnapshot` utility for per-partition offset assertions
  (T024).
- **Amazon SQS**: `SqsEventSender.SendAsync(SendContext)` routes `SessionKey` →
  `MessageGroupId` and `DeduplicationKey` → `MessageDeduplicationId` for FIFO queues (T030).
  `IQueueTopologyBuilder` + `SqsTopologyBuilder` (standard + FIFO + DLQ binding);
  `SqsRigBuilder.WithTopology` hook (T031/T032).
- Architecture guard: `ProviderCompletenessTests` enforces `WithTopology`, `SendContext`
  overload, and session-listener invariants for every assembly listed in
  `.parity-coverage.txt` (FR-007-08).

### Added — Feature 007 Phase 5 (planned release N+3: NATS JetStream)

- **NATS JetStream**: `NatsJetStreamFixture` — Testcontainers-backed fixture exposing an
  `INatsJSContext` (`JetStream` property), `EnsureStreamAsync(name, subjects, maxMsgs?, ct)` with
  idempotent create-or-update semantics, and `GetStreamAsync(name, ct)` (T051).
  `NatsJetStreamEventSender` publishes via `INatsJSContext.PublishAsync` and writes
  `x-session-key` NATS header when `SendContext.SessionKey` is set (T052).
  `NatsJetStreamListener` creates an ordered consumer per `NatsJSOrderedConsumerOpts`
  with optional `FilterSubjects`, reads `x-session-key` into `CapturedMessage.SessionKey`,
  and acknowledges each message via `AckAsync` (T053).
  `INatsTopologyBuilder` + `NatsTopologyBuilder` (stream + subjects + max-messages +
  retention-policy via `NatsRetentionPolicy.{Limits,Interest,WorkQueue}`); idempotency via
  `NatsJSApiException(Code == 400)` → `UpdateStreamAsync` (T054).
  `NatsRigBuilder.WithTopology` hook + `ApplyTopologyAsync`; `UseNats(NatsJetStreamFixture, …)`
  extension overload passes `fixture.JetStream` into the builder.
- Architecture parity: `Rig.TUnit.Messaging.Nats` appended to `.parity-coverage.txt` —
  all 5 messaging providers now at full Feature-007 parity.
  `DependencyDirectionTests.NatsJetStream_ReferencedOnlyByNatsProvider` guards against
  accidental `NATS.Client.JetStream` leakage into other provider assemblies.

### Added — Feature 007 Phase 4 (planned release N+2: RabbitMQ topology)

- **RabbitMQ**: `RabbitMqEventSender.SendAsync(SendContext)` sets `RoutingKey` from
  `PartitionKey ?? SessionKey` and propagates deduplication key via `MessageId` header (T040).
  `RabbitMqSessionListener` reads `x-session-key` from AMQP message headers and surfaces it
  in `CapturedMessage.SessionKey` (T041). `IRabbitMqTopologyBuilder` + `RabbitMqTopologyBuilder`
  supporting topic-exchange fan-out with `BindQueue(queue, routingKey)`, DLX routing via
  `WithDeadLetterExchange`, per-queue `WithMaxPriority` for priority queues, and
  `WithQuorumQueue()` for HA quorum queues (T042/T043). `RabbitMqRigBuilder.WithTopology` hook.
- Architecture parity: `Rig.TUnit.Messaging.RabbitMq` appended to `.parity-coverage.txt`;
  `ProviderCompletenessTests` suite extends coverage to RabbitMQ topology + session-listener
  invariants.

### Added — Feature 005 (Legacy Coverage & Docs Parity)
- MTP-native `--coverage` collection on every `integration-*` CI job (FR-020).
- `coverage-summary` CI job merging cobertura → Html + Markdown via ReportGenerator,
  publishing to `$GITHUB_STEP_SUMMARY`, and uploading a 30-day `coverage-report` artefact (FR-021).
- Per-package coverage threshold gate (`line-rate ≥ 0.90`, `branch-rate ≥ 0.85`) enforced
  on every PR (FR-022 — non-blocking in Phase 2, blocking from T069b).
- `benchmarks/coverage-baseline-005.json` + `benchmarks/baseline-005.json` schemas (FR-023, FR-037).
- Missing test categories filled for pre-004 providers:
  - Integration + Contract for Rig.TUnit.Core (T021).
  - Integration + Contract + Benchmark for Rig.TUnit.Mediator, Grpc, WebAPI, Http (T023-T029).
  - Unit and/or Benchmark for Rig.TUnit.Ci, Concurrency, HealthChecks, Parallelism, Resilience
    (T031, T033, T035, T037, T039).
  - Unit and/or Benchmark for Caching.Memory/Redis, Databases.Sql.Sqlite/SqlServer,
    Databases.NoSql.Redis (T041, T043, T045, T047, T049).
  - Unit and Benchmark for Observability.Logging/Seq/Tracing (T051, T053, T055).
  - Unit and Benchmark for Microservices.{Contracts, Saga, Inbox, Outbox, Snapshots}
    (T057-T065).
- `PostgresDbContextHelper.CreateEphemeralDatabaseAsync` — per-test physical database
  isolation for Postgres tests, resolving the master-CI flake (T004, FR-010, SC-001).
- `OrphanFolderTests` — regression guard for pre-004 path deletions (T002, FR-012).
- `ArtifactUploadTests` + per-job `upload-artifact@v4` step with 14-day retention (FR-013).
- `CoverageCollectionTests`, `CoverageSummaryJobTests`, `CoverageThresholdTests` — YAML
  assertions enforcing the coverage pipeline shape.
- `NoSkipMarkersTests` + `SharedFixtureGuardTests` — enforcement rules that prevent
  regression of the FR-004 / FR-011 invariants (T104b/T104c, SC-012/SC-013).
- Phase-1 minimal `commit-discipline-gate` CI job checking RED→GREEN subject pairing
  on every PR (partial FR-002; full hardening in Phase 7).
- Root governance files: `LICENSE` (MIT), `SECURITY.md`, `CHANGELOG.md` (this file),
  updated `CONTRIBUTING.md`, rewritten `README.md` (T121, FR-060).

### Changed
- `UsePostgresFluentTests` now acquires per-test ephemeral databases — every test owns
  its physical DB, so schema-inspection assertions are deterministic under parallel execution.
- Every `integration-*` matrix job's `dotnet test` invocation gains
  `-- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml`.
- `Shared*Fixture.cs` files across 20 test projects gain an
  `Intentional reuse per A005 audit` rationale comment (FR-011).

### Deprecated
- Consumer projects using `coverlet.msbuild` should migrate to the MTP-native
  `--coverage` collector. The package pin is retained in
  `Directory.Packages.props` only for backwards compatibility with scripts that haven't
  migrated yet. See `CONTRIBUTING.md` § Coverage gate.

### Removed
- Orphan folders `src/Rig.TUnit.ServiceBus/`, `tests/Rig.TUnit.ServiceBus.Tests.Integration/`,
  `tests/Rig.TUnit.SqlServer.Tests.Integration/` (T002 — already empty of tracked content;
  OrphanFolderTests enforces they stay gone).

### Fixed
- Postgres integration flake on master CI — `42P01: relation "Samples" does not exist`
  under parallel test execution (FR-010, SC-001, T003/T004).

## [0.4.0] — 2026-04-19 — Feature 004 (Provider Consistency Remediation)
- Canonical quartet (Fixture + FixtureOptions + RigBuilder + Use{Provider}) enforced
  across 32 providers via `ProviderCompletenessTests`.
- Test-pyramid completeness (`Unit + Integration + Contract + Benchmark`) per leaf
  provider via `TestCompletenessTests` (FR-031 onwards).
- `DatabasePerTestHelper` pattern established for SQL providers.

## [0.3.0] — 2026-03 — Feature 003 (Ecosystem Expansion)
- Hard cutover to the `Rig.TUnit.{Family}.{Provider}` naming convention.
- 14 new base + provider packages added across Databases, Messaging, Caching,
  Storage, Security, and Observability families.
- **Breaking rename:** `EventStore.Client.Grpc.Streams` → `KurrentDB.Client` per the
  Kurrent rebrand. `Rig.TUnit.Databases.NoSql.EventStore` is retired; consumers migrate
  to `Rig.TUnit.Databases.NoSql.KurrentDb`. Scripts that pinned the old NuGet package
  must update. See
  [planning/post-004-remediation/Documentation-Audit.md](planning/post-004-remediation/Documentation-Audit.md)
  for the migration log.

## [0.2.0] — 2026-02 — Feature 002 (Fluent Builder Expansion)
- `Rig.TUnit.WebAPI` package with `WebApplicationFactory`-backed `HttpClientHelper`.
- Test-authentication helpers for bearer-token scenarios.
- All provider RigBuilders unified under the CRTP `{Family}RigBuilder<TSelf>` shape.

## [0.1.0] — 2026-01 — Feature 001 (Rig.TUnit Library)
- Initial release: `Rig.TUnit.Core` with `RigBuilder`, `RigConnect`, `IsolationKey`.
- First container-backed fixtures for SqlServer + Redis.

[Unreleased]: https://github.com/FaysilAlshareef/Rig.TUnit/compare/v0.1.0-beta.2...HEAD
[0.1.0-beta.2]: https://github.com/FaysilAlshareef/Rig.TUnit/compare/v0.1.0-beta.1...v0.1.0-beta.2
[0.1.0-beta.1]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.1.0-beta.1
[0.4.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.4.0
[0.3.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.3.0
[0.2.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.2.0
[0.1.0]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.1.0
