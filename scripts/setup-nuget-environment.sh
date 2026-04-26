#!/usr/bin/env bash
# Configure the protected GitHub environment that gates nuget.org publishes.
# Run AFTER you have completed Trusted Publishing setup on nuget.org
# (Manage Account -> Trusted Publishing -> Add new with environment "nuget-org").
#
#   bash scripts/setup-nuget-environment.sh
#
# What it does:
#   1. Creates the `nuget-org` environment
#   2. Adds the repo owner as the sole reviewer
#   3. Restricts deployment to master branch + v* tags

set -euo pipefail

REPO="${RIG_TUNIT_REPO:-FaysilAlshareef/Rig.TUnit}"

echo "==> Create nuget-org environment with owner as reviewer"
USER_ID=$(gh api user --jq .id)
gh api -X PUT "repos/$REPO/environments/nuget-org" \
  -F wait_timer=0 \
  -F deployment_branch_policy[protected_branches]=false \
  -F deployment_branch_policy[custom_branch_policies]=true \
  -F "reviewers[][type]=User" \
  -F "reviewers[][id]=$USER_ID"

echo "==> Restrict deployments to master + v* tags"
gh api -X POST "repos/$REPO/environments/nuget-org/deployment-branch-policies" \
  -f name=master -f type=branch || true
gh api -X POST "repos/$REPO/environments/nuget-org/deployment-branch-policies" \
  -f name='v*' -f type=tag || true

echo ""
echo "Done. Verify with:"
echo "  gh api repos/$REPO/environments/nuget-org"
echo ""
echo "Reminder: complete the nuget.org side at https://www.nuget.org/account/trusted-publishers"
echo "  Publisher:        GitHub Actions"
echo "  Repository owner: ${REPO%/*}"
echo "  Repository name:  ${REPO#*/}"
echo "  Workflow:         release.yml"
echo "  Environment:      nuget-org"
echo "  Package glob:     Rig.TUnit*"
