# scripts/

One-shot scripts used during release readiness and ongoing repository hygiene.
None of these run on every build - they are operator tools you run by hand
when an event warrants it.

| Script | When to run | What it does |
|---|---|---|
| `install-coc.sh` | Once before publishing the first release; whenever the Contributor Covenant text changes | Fetches the canonical Contributor Covenant 2.1 from `contributor-covenant.org`, substitutes the project contact, writes it to `CODE_OF_CONDUCT.md` |
| `apply-package-descriptions.ps1` | Once during the metadata sweep; later only if new csproj projects are added | Idempotently injects `<Description>` into every `src/**/*.csproj` based on the mapping table inside the script |
| `apply-repo-settings.sh` | Once during release readiness; whenever the label set or topics change | Sets repo description / topics / discussions; enables vulnerability alerts, Dependabot security updates, secret scanning, push protection; configures merge settings; applies the 28-label triage scheme |
| `apply-branch-protection.sh` | After the CI refactor PR has merged on `master` | Applies the `master-protection` ruleset (1 CODEOWNERS approval, signed status checks, linear history) and `release-tag-protection` (only admins can push `v*` tags) |
| `setup-nuget-environment.sh` | After registering Trusted Publishing on nuget.org | Creates the protected `nuget-org` GitHub environment, adds the repo owner as the sole reviewer, restricts deployments to `master` + `v*` tags |

## Order of operations for a fresh release-ready setup

```bash
bash   scripts/install-coc.sh
pwsh   scripts/apply-package-descriptions.ps1     # (already run)
bash   scripts/apply-repo-settings.sh
# Wait for the CI refactor PR to merge.
bash   scripts/apply-branch-protection.sh
# Register Trusted Publishing on nuget.org.
bash   scripts/setup-nuget-environment.sh
# Cut v0.1.0-beta.1 - see docs/RELEASING.md.
```

Every script is idempotent and safe to re-run. They use `gh` CLI authenticated
as the repository owner; set `RIG_TUNIT_REPO=<owner>/<repo>` to target a fork.
