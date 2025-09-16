# Auto-Approve Workflow Runs - Solutions

This documents solutions for the "Approve workflows to run" bottleneck in our automation pipeline.

## Problem
When Copilot creates PRs, GitHub requires manual approval for workflows to run due to security restrictions for first-time contributors and external apps.

## Solution 1: Workflow-Based Auto-Approval (Implemented)

### File: `.github/workflows/auto-approve-workflow-runs.yml`

**How it works:**
1. Triggers on `pull_request_target` (has full permissions)
2. Checks if PR is from Copilot and RFC-related
3. Uses `AUTO_APPROVE_TOKEN` to approve pending workflow runs
4. Waits and confirms workflows start running

**Requirements:**
- `AUTO_APPROVE_TOKEN` secret with `actions:write` permission
- Token must be from a user with admin/write access to the repository

## Solution 2: Repository Settings (Manual Setup)

### GitHub Repository Settings
Navigate to: `Settings → Actions → General → Fork pull request workflows`

**Option A: Auto-approve for collaborators**
- Set "Require approval for all outside collaborators"
- Add Copilot as a collaborator

**Option B: Auto-approve for specific users**
- Go to "Fork pull request workflows from outside collaborators"
- Add `Copilot` and `app/copilot-swe-agent` to trusted contributors

### Branch Protection Rules
In `Settings → Branches → main`:
- Enable "Restrict pushes that create files"
- Add exception for `Copilot` user

## Solution 3: PAT Token Setup

### Required Token Permissions
For the `AUTO_APPROVE_TOKEN` secret:
```
actions:write    # Approve workflow runs
contents:read    # Read repository content
pull-requests:read  # Read PR information
```

### Token Creation
1. GitHub Settings → Developer settings → Personal access tokens → Fine-grained tokens
2. Repository access: Select `ApprenticeGC/ithome-ironman-2025`
3. Permissions: `actions:write`, `contents:read`, `pull-requests:read`
4. Add as repository secret: `AUTO_APPROVE_TOKEN`

## Testing the Solution

### Expected Flow After Implementation:
```
1. Copilot creates PR → triggers auto-approve-workflow-runs.yml
2. auto-approve-workflow-runs.yml approves pending workflows
3. CI and other workflows start automatically
4. auto-ready-pr.yml marks PR ready after CI success
5. auto-approve-merge.yml approves and merges PR
```

### Verification Commands:
```bash
# Check for pending workflow runs
gh api repos/ApprenticeGC/ithome-ironman-2025/actions/runs --jq '.workflow_runs[] | select(.status == "action_required")'

# Check workflow run status for PR
gh pr checks 94 --repo ApprenticeGC/ithome-ironman-2025

# Monitor workflow runs
gh run list --repo ApprenticeGC/ithome-ironman-2025 --limit 5
```

## Implementation Status

- ✅ **Workflow created**: `auto-approve-workflow-runs.yml`
- ⏳ **Token setup**: Requires `AUTO_APPROVE_TOKEN` secret configuration
- ⏳ **Testing**: Needs validation with next Copilot PR

## Next Steps

1. Configure `AUTO_APPROVE_TOKEN` secret in repository settings
2. Test with a new RFC micro-issue to validate end-to-end flow
3. Monitor for any remaining manual approval requirements
4. Document successful automation timing benchmarks
