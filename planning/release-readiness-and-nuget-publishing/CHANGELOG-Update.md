# `CHANGELOG.md` update for `v0.1.0-beta.1`

This file contains the **exact text** to insert into `CHANGELOG.md` when cutting the
`v0.1.0-beta.1` tag. Replace `YYYY-MM-DD` with the actual release date.

---

## Edit instructions

1. Open `CHANGELOG.md`.
2. Locate the `## [Unreleased]` heading.
3. Insert the new `## [0.1.0-beta.1]` section **directly above** `## [Unreleased]`,
   keeping `[Unreleased]` empty for future entries.
4. At the bottom of the file, add the version comparison link (see "Comparison links" below).
5. Stage the change as part of the `release: v0.1.0-beta.1` commit; tag points at this commit.

---

## New section to insert

```markdown
## [0.1.0-beta.1] — YYYY-MM-DD

First public preview. **Pre-1.0: APIs may change before `1.0.0`.**

### Added — Release readiness & open-source onboarding

- **NuGet publishing pipeline** — tag-driven, OIDC-trusted publishing to nuget.org via
  `release.yml`; GitHub Packages mirror; deterministic builds, embedded sources, symbol
  packages (`.snupkg`). Approval-gated through the protected `nuget-org` GitHub environment.
- **Versioning** — MinVer drives `<Version>` from the latest `v*` git tag; untagged builds
  produce `0.0.0-alpha.0.{height}.{sha}`.
- **Source Link** — `Microsoft.SourceLink.GitHub` enables step-into debugging from any
  consumer that has Source Link enabled in Visual Studio / Rider / VS Code.
- **Per-package metadata** — every `src/**` package ships with a `<Description>`, repo URL,
  project URL, MIT license expression, README, icon, and tag list.
- **Branch protection** — `master` requires a PR with one CODEOWNERS approval, all required
  status checks green, conversation resolution, and linear history. Force pushes and
  deletions are blocked. Tags `v*` are protected from non-admin pushes.
- **Community files** — `CODE_OF_CONDUCT.md` (Contributor Covenant 2.1), `CODEOWNERS`,
  `.github/PULL_REQUEST_TEMPLATE.md`, `.github/ISSUE_TEMPLATE/{bug,feature,provider,docs}.yml`,
  `.github/DISCUSSION_TEMPLATE/{q-and-a,show-and-tell,ideas}.yml`, `.github/FUNDING.yml`,
  `.github/dependabot.yml`.
- **Repository labels** — 28-label triage scheme (type:_, priority:_, status:_, provider:_,
  good-first-issue, help-wanted, breaking-change, dependencies, release:major/minor/patch).
- **Security workflows** — secret scanning + push protection enabled; CodeQL C# weekly +
  on-PR; Dependabot weekly NuGet (grouped) + GitHub Actions; stale-issue bot.
- **CI optimization** — workflow concurrency cancellation; NuGet cache (`actions/cache` keyed
  on `Directory.Packages.props` + `**/*.csproj` hash); single warmup-restore feeds matrix
  jobs via uploaded `obj/`+`bin/` artefact; per-matrix `paths-filter` so e.g. SQL-only PRs
  skip NoSQL/messaging/caching matrices; reusable `setup-dotnet-cache` composite action;
  link checking consolidated to lychee; `architecture-tests` folded into `build-unit-arch`.
  Net effect: ~40 % CI wall-clock reduction on typical PRs.
- **`pack-validate` CI job** — every PR runs `dotnet pack` and fails when a packable project
  regresses on description/license/readme/icon metadata.
- **Discussions** enabled; bug/feature/provider/docs forms wired through
  `.github/ISSUE_TEMPLATE/config.yml`.
- **Release Drafter** — populates draft release notes from merged PR labels between releases.

### Changed

- `Directory.Build.props` extended with shared NuGet packaging properties; production code
  in `src/**` opts in to packing via `src/Directory.Build.props` (`<IsPackable>true</IsPackable>`);
  tests/benchmarks remain non-packable via `tests/Directory.Build.props`.
- `Directory.Packages.props` adds `MinVer` and `Microsoft.SourceLink.GitHub` central versions.
- `CONTRIBUTING.md` documents the required-status-check list and the release ritual.
- `CI` workflow refactored — see `planning/release-readiness-and-nuget-publishing/CI-Refactor-Plan.md`
  for the diff summary.

### Removed

- Duplicate markdown link checker (`gaurav-nelson/github-action-markdown-link-check`) — lychee
  remains as the single link-check job.
- `red-commit-verification` no-op job (was emitting notices only) — `commit-discipline-gate`
  retains the RED→GREEN pairing check.

### Security

- Secret scanning + push protection enabled.
- CodeQL C# weekly scan; zero P0 findings on first run.
- Dependabot weekly NuGet + GitHub Actions audit.
- Trusted Publishing to nuget.org (OIDC) — no long-lived API keys in repository secrets.

### Public package surface (initial drop)

70 packages across 9 families. Full inventory:

**Core**: `Rig.TUnit.Core`.
**Meta-packages**: `Rig.TUnit`, `Rig.TUnit.All`.
**SQL**: `Rig.TUnit.Databases.Sql{,.SqlServer,.MySql,.Postgresql,.Oracle,.Sqlite}`.
**NoSQL**: `Rig.TUnit.Databases.NoSql{,.Redis,.Mongo,.Cosmos,.Cassandra,.Dynamo,.ElasticSearch,.KurrentDb}`.
**Messaging**: `Rig.TUnit.Messaging{,.ServiceBus,.Kafka,.RabbitMq,.Nats,.Sqs}`.
**Caching**: `Rig.TUnit.Caching{,.Redis,.Memory,.Hybrid,.Fusion}`.
**Storage**: `Rig.TUnit.Storage{,.AzureBlob,.FileSystem,.MinIO,.S3}`.
**Observability**: `Rig.TUnit.Observability{,.Logging,.Logging.Analyzers,.Metrics,.Tracing,.Seq,.AppInsights}`.
**Security**: `Rig.TUnit.Security{,.Jwt,.OAuth,.Mtls,.Policies}`.
**Microservices**: `Rig.TUnit.Microservices{,.EventSourcing,.Outbox,.Inbox,.Saga,.Snapshots,.Contracts}`.
**Infrastructure**: `Rig.TUnit.{Http,Grpc,HealthChecks,Resilience,Mediator,Docker,Parallelism,Concurrency,Ci,WebAPI}`.

### Pre-release content carried over from `[Unreleased]`

The Feature 007 (Messaging Topology & Sessions) entries previously listed under
`[Unreleased]` are included in this release — see the existing `[Unreleased]` text for the
full Phase 0–6 detail; that block moves into this section unchanged.
```

---

## Comparison links (append at bottom of `CHANGELOG.md`)

```markdown
[Unreleased]: https://github.com/FaysilAlshareef/Rig.TUnit/compare/v0.1.0-beta.1...HEAD
[0.1.0-beta.1]: https://github.com/FaysilAlshareef/Rig.TUnit/releases/tag/v0.1.0-beta.1
```

If a `[Unreleased]: …` link already exists, replace its target ref with `v0.1.0-beta.1...HEAD`
and add the new `[0.1.0-beta.1]` link below it.

---

## Sanity-check before tagging

- [ ] `[Unreleased]` heading present and empty above the new `[0.1.0-beta.1]` heading
- [ ] Date placeholder `YYYY-MM-DD` replaced with the real release date
- [ ] Comparison link added at the bottom
- [ ] Feature 007 content from the old `[Unreleased]` block has been folded into the new
      `[0.1.0-beta.1]` section (do **not** double-list)
- [ ] `markdown-link-check` passes on the updated file (run locally or wait for CI)
- [ ] Commit message: `release: v0.1.0-beta.1`
- [ ] After commit, tag with `git tag -a v0.1.0-beta.1 -m "0.1.0-beta.1"` and push

---

## Future-changelog conventions (forward-looking)

For subsequent releases, the `[Unreleased]` section accumulates entries under these subheadings,
in this order, omitting any that are empty:

```
### Added
### Changed
### Deprecated
### Removed
### Fixed
### Security
```

When cutting a release:
1. Rename `[Unreleased]` → `[N.N.N] — YYYY-MM-DD`.
2. Insert a fresh empty `[Unreleased]` above it.
3. Update the comparison links at the bottom.
4. Commit as `release: vN.N.N`, tag, push.

`release.yml` extracts the body of the matched `[N.N.N]` section and uses it as the GitHub
Release body (replacing the `Release-Notes-vN.N.N.md` file once the convention is
established — for `0.1.0-beta.1` the curated `Release-Notes-v0.1.0-beta.1.md` is the body
because the launch narrative is broader than the changelog entry).
