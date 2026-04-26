#!/usr/bin/env bash
# One-time repository settings, topics, security flags, and label set for Rig.TUnit.
# Idempotent - safe to re-run. Requires gh CLI authenticated as the repo owner.
#
#   bash scripts/apply-repo-settings.sh
#
# What it does:
#   1. Sets description, homepage, topics; enables Discussions; disables Wiki
#   2. Enables vulnerability alerts + Dependabot security updates + secret scanning
#   3. Configures merge settings (squash + rebase, auto-merge, delete head branches)
#   4. Applies the 28-label triage scheme (deletes the four GitHub defaults we don't use)

set -euo pipefail

REPO="${RIG_TUNIT_REPO:-FaysilAlshareef/Rig.TUnit}"

echo "==> Repo settings: description, topics, discussions"
gh repo edit "$REPO" \
  --description "TUnit-first integration-testing rig for .NET - fixtures, builders, and assertions for SQL, NoSQL, messaging, caching, storage, observability, security, and microservices." \
  --homepage "https://github.com/$REPO" \
  --enable-issues=true \
  --enable-discussions=true \
  --enable-wiki=false \
  --enable-projects=false

echo "==> Topics"
for t in dotnet csharp testing tunit integration-testing testcontainers \
         test-fixtures xunit-alternative nuget microservices-testing \
         ef-core dotnet10; do
  gh repo edit "$REPO" --add-topic "$t" || true
done

echo "==> Security: vulnerability alerts + Dependabot security updates"
gh api -X PUT "repos/$REPO/vulnerability-alerts" || true
gh api -X PUT "repos/$REPO/automated-security-fixes" || true

echo "==> Security: secret scanning + push protection"
gh api -X PATCH "repos/$REPO" \
  -F security_and_analysis[secret_scanning][status]=enabled \
  -F security_and_analysis[secret_scanning_push_protection][status]=enabled \
  || echo "  (note: secret scanning requires Advanced Security on private repos)"

echo "==> Merge settings: squash + rebase, auto-merge on, delete head branches"
gh api -X PATCH "repos/$REPO" \
  -F allow_squash_merge=true \
  -F allow_rebase_merge=true \
  -F allow_merge_commit=false \
  -F allow_auto_merge=true \
  -F delete_branch_on_merge=true \
  -F allow_update_branch=true \
  -F squash_merge_commit_title=PR_TITLE \
  -F squash_merge_commit_message=PR_BODY \
  -F use_squash_pr_title_as_default=true

echo "==> Labels"
# Type
gh label create "type:bug"               --color d73a4a --description "Defect or regression" --force
gh label create "type:feature"           --color a2eeef --description "New capability" --force
gh label create "type:provider"          --color 5319e7 --description "New or expanded provider" --force
gh label create "type:docs"              --color 0075ca --description "Documentation" --force
gh label create "type:perf"              --color ff8800 --description "Performance" --force
gh label create "type:test"              --color d4c5f9 --description "Tests / coverage / fakers" --force
gh label create "type:ci"                --color ededed --description "CI / tooling / build" --force
gh label create "type:security"          --color b60205 --description "Security-relevant" --force

# Priority
gh label create "priority:critical"      --color b60205 --description "Drop everything" --force
gh label create "priority:high"          --color d93f0b --description "Next release" --force
gh label create "priority:medium"        --color fbca04 --description "Soon" --force
gh label create "priority:low"           --color c2e0c6 --description "Whenever" --force

# Status
gh label create "status:needs-triage"    --color ededed --description "Awaiting triage" --force
gh label create "status:needs-repro"     --color fef2c0 --description "Reproduction needed" --force
gh label create "status:blocked"         --color 000000 --description "Blocked on dependency" --force
gh label create "status:in-progress"     --color 1d76db --description "Actively being worked" --force
gh label create "status:stale"           --color cccccc --description "Idle - subject to auto-close" --force

# Provider families
gh label create "provider:sql"           --color 0e8a16 --force
gh label create "provider:nosql"         --color 0e8a16 --force
gh label create "provider:messaging"     --color 0e8a16 --force
gh label create "provider:caching"       --color 0e8a16 --force
gh label create "provider:storage"       --color 0e8a16 --force
gh label create "provider:observability" --color 0e8a16 --force
gh label create "provider:security"      --color 0e8a16 --force
gh label create "provider:microservices" --color 0e8a16 --force

# Community
gh label create "good-first-issue"       --color 7057ff --description "Suitable for first-time contributors" --force
gh label create "help-wanted"            --color 008672 --description "Extra attention welcome" --force
gh label create "breaking-change"        --color b60205 --description "Breaks public API or behaviour" --force
gh label create "dependencies"           --color 0366d6 --description "Dependency updates" --force

# Release-drafter buckets
gh label create "release:major"          --color b60205 --description "Bumps the major version" --force
gh label create "release:minor"          --color 0e8a16 --description "Bumps the minor version" --force
gh label create "release:patch"          --color c2e0c6 --description "Bumps the patch version" --force

# Cleanup of GitHub default labels we don't use
for stale in invalid wontfix duplicate question; do
  gh label delete "$stale" --yes 2>/dev/null || true
done

echo ""
echo "Done. Verify with:"
echo "  gh repo view $REPO --json description,repositoryTopics,hasDiscussionsEnabled"
echo "  gh label list --limit 100"
