# Repository Settings

Phase A-1 / A-2 / A-9. Run all of the following from a shell with `gh` authenticated as
the repo owner. Idempotent — safe to re-run.

---

## 1 · Description, homepage, topics

```bash
gh repo edit FaysilAlshareef/Rig.TUnit \
  --description "TUnit-first integration-testing rig for .NET — fixtures, builders, and assertions for SQL, NoSQL, messaging, caching, storage, observability, security, and microservices." \
  --homepage "https://github.com/FaysilAlshareef/Rig.TUnit" \
  --enable-issues=true \
  --enable-discussions=true \
  --enable-wiki=false \
  --enable-projects=false

# Topics (12) — discoverability on GitHub Explore + nuget.org search
gh repo edit FaysilAlshareef/Rig.TUnit \
  --add-topic dotnet \
  --add-topic csharp \
  --add-topic testing \
  --add-topic tunit \
  --add-topic integration-testing \
  --add-topic testcontainers \
  --add-topic test-fixtures \
  --add-topic xunit-alternative \
  --add-topic nuget \
  --add-topic microservices-testing \
  --add-topic ef-core \
  --add-topic dotnet10
```

---

## 2 · Security flags

```bash
# Enable Dependabot vulnerability alerts (security advisories)
gh api -X PUT repos/FaysilAlshareef/Rig.TUnit/vulnerability-alerts

# Enable Dependabot security updates (auto-PRs for known CVEs)
gh api -X PUT repos/FaysilAlshareef/Rig.TUnit/automated-security-fixes

# Enable secret scanning + push protection
gh api -X PATCH repos/FaysilAlshareef/Rig.TUnit \
  -F security_and_analysis[secret_scanning][status]=enabled \
  -F security_and_analysis[secret_scanning_push_protection][status]=enabled
```

If any of the above returns `403 Advanced Security must be enabled`, the repo is private —
secret scanning needs Advanced Security on private repos. For this public repo it is free.

---

## 3 · Merge settings

Lock in: squash + rebase only, auto-delete head branches, allow auto-merge.

```bash
gh api -X PATCH repos/FaysilAlshareef/Rig.TUnit \
  -F allow_squash_merge=true \
  -F allow_rebase_merge=true \
  -F allow_merge_commit=false \
  -F allow_auto_merge=true \
  -F delete_branch_on_merge=true \
  -F allow_update_branch=true \
  -F squash_merge_commit_title=PR_TITLE \
  -F squash_merge_commit_message=PR_BODY \
  -F use_squash_pr_title_as_default=true
```

---

## 4 · Label set

The script applies idempotently — `gh label create … --force` overwrites colour/description if
the label already exists.

```bash
# --- Type ---
gh label create "type:bug"               --color d73a4a --description "Defect or regression" --force
gh label create "type:feature"           --color a2eeef --description "New capability" --force
gh label create "type:provider"          --color 5319e7 --description "New or expanded provider" --force
gh label create "type:docs"              --color 0075ca --description "Documentation" --force
gh label create "type:perf"              --color ff8800 --description "Performance" --force
gh label create "type:test"              --color d4c5f9 --description "Tests / coverage / fakers" --force
gh label create "type:ci"                --color ededed --description "CI / tooling / build" --force
gh label create "type:security"          --color b60205 --description "Security-relevant" --force

# --- Priority ---
gh label create "priority:critical"      --color b60205 --description "Drop everything" --force
gh label create "priority:high"          --color d93f0b --description "Next release" --force
gh label create "priority:medium"        --color fbca04 --description "Soon" --force
gh label create "priority:low"           --color c2e0c6 --description "Whenever" --force

# --- Status ---
gh label create "status:needs-triage"    --color ededed --description "Awaiting triage" --force
gh label create "status:needs-repro"     --color fef2c0 --description "Reproduction needed" --force
gh label create "status:blocked"         --color 000000 --description "Blocked on dependency" --force
gh label create "status:in-progress"     --color 1d76db --description "Actively being worked" --force

# --- Provider families ---
gh label create "provider:sql"           --color 0e8a16 --force
gh label create "provider:nosql"         --color 0e8a16 --force
gh label create "provider:messaging"     --color 0e8a16 --force
gh label create "provider:caching"       --color 0e8a16 --force
gh label create "provider:storage"       --color 0e8a16 --force
gh label create "provider:observability" --color 0e8a16 --force
gh label create "provider:security"      --color 0e8a16 --force
gh label create "provider:microservices" --color 0e8a16 --force

# --- Community ---
gh label create "good-first-issue"       --color 7057ff --description "Suitable for first-time contributors" --force
gh label create "help-wanted"            --color 008672 --description "Extra attention welcome" --force
gh label create "breaking-change"        --color b60205 --description "Breaks public API or behaviour" --force
gh label create "dependencies"           --color 0366d6 --description "Dependency updates" --force

# --- Release-drafter buckets ---
gh label create "release:major"          --color b60205 --description "Bumps the major version" --force
gh label create "release:minor"          --color 0e8a16 --description "Bumps the minor version" --force
gh label create "release:patch"          --color c2e0c6 --description "Bumps the patch version" --force

# --- Cleanup of GitHub default labels we don't use ---
for stale in invalid wontfix duplicate question; do
  gh label delete "$stale" --yes 2>/dev/null || true
done
```

---

## 5 · Verification

```bash
gh repo view FaysilAlshareef/Rig.TUnit --json name,description,homepageUrl,repositoryTopics,hasDiscussionsEnabled,hasIssuesEnabled,hasWikiEnabled
gh label list --limit 100
gh api repos/FaysilAlshareef/Rig.TUnit | jq '.security_and_analysis'
```

Expected after Phase A:

- `description` non-empty
- `repositoryTopics` contains 12 topics
- `hasDiscussionsEnabled = true`, `hasWikiEnabled = false`, `hasIssuesEnabled = true`
- `security_and_analysis.secret_scanning.status = "enabled"`
- `security_and_analysis.secret_scanning_push_protection.status = "enabled"`
- `gh label list` shows 28 labels
