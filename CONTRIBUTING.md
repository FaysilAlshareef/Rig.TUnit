<!-- STUB: replaced by Phase 6a T121 -->

# Contributing (stub — full content in Phase 6a)

Feature 005 Phase 2 ships this file as a minimal stub so the coverage gate
has a landing place reviewers can find from the README. Phase 6a T121 (`docs(005)`)
replaces it with the full OSS-ready governance version: TDD rules, skip-forbidden
policy, canonical-template pointer, PR checklist, `markdown-link-check` allow-list,
and the full gate-set Phase 7 T174 depends on.

**Anything below this banner that contradicts Phase 6a's rewrite should be
taken as partial / provisional.**

---

## Coverage gate

Feature 005 enforces a per-package coverage floor on every PR via the
`coverage-summary` CI job:

- **line-rate ≥ 0.90**
- **branch-rate ≥ 0.85**

### Collecting coverage locally

Use the **MTP-native collector** — `dotnet test`'s own runner forwards arguments
after `--` to `Microsoft.Testing.Platform`:

```sh
dotnet test tests/Rig.TUnit.<Provider>.Tests.Integration/ \
    --no-build -c Release \
    -- --coverage \
       --coverage-output-format cobertura \
       --coverage-output coverage.cobertura.xml
```

The cobertura file lands next to the TRX / HTML report under
`tests/<project>/bin/Release/net10.0/TestResults/`.

> **Do NOT use `coverlet.msbuild`.** It collides with the MTP-native collector
> per [`planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md`](planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md)
> — `coverlet.msbuild` wraps the pre-MTP xUnit pipeline and produces empty
> cobertura when MTP's own harness is also enabled. The pin stays in
> `Directory.Packages.props` only for backwards-compatibility with scripts that
> haven't migrated yet. New code MUST use the MTP path.

### Merging cobertura across projects

The CI `coverage-summary` job fans in every matrix job's cobertura artefact and
renders a merged HTML + Markdown report via ReportGenerator. To reproduce locally:

```sh
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
    "-reports:./tests/**/TestResults/**/coverage.cobertura.xml" \
    "-targetdir:./coverage-report" \
    "-reporttypes:Html;Cobertura;MarkdownSummaryGithub"
```

Open `./coverage-report/index.html` to review per-package rates.

### Phase 2 non-blocking → Phase 3 blocking

Phase 2 (T015) ships the threshold step with `continue-on-error: true` — it
annotates offending packages but does NOT fail the build. This is deliberate:
Phase 3 (T020-T068) raises each provider's coverage to the bar, and **T069b**
flips `continue-on-error: false` at Phase 3 close.

Once flipped, any PR that drops a package below the floor fails the
`coverage-summary` job and blocks merge.

---

## See also

- [`planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md`](planning/post-004-remediation/CI-Artifact-And-Coverage-Proposal.md) — design rationale
- [`.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/spec.md`](.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/spec.md) — full FR set
- [`benchmarks/coverage-baseline-005.json`](benchmarks/coverage-baseline-005.json) — per-package baseline
