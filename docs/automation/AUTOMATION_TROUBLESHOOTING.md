# Automation Flow Troubleshooting Guide

**Quick Reference for GitHub Copilot and Other Agents**

## 🚨 Common Issues & Solutions

### Issue: PR Shows "Unstable but Mergeable"
**Symptoms**: Auto-merge blocked, multiple check runs visible
**Root Cause**: Duplicate CI workflows creating merge state confusion
**Solution**: Direct merge workflow automatically handles this

**Debug Commands**:
```bash
# Check merge status
gh pr view $PR_NUMBER --json mergeStateStatus,mergeable,mergeableState

# Count check runs for commit
gh api "repos/ApprenticeGC/ithome-ironman-2025/commits/$COMMIT_SHA/check-runs" | jq '.check_runs | length'

# Look for duplicate build_test runs
gh api "repos/ApprenticeGC/ithome-ironman-2025/commits/$COMMIT_SHA/check-runs" | jq '.check_runs[] | {name, conclusion, started_at}'
```

### Issue: CI Keeps Re-dispatching
**Symptoms**: Multiple workflow runs for same branch, rapid successive dispatches
**Root Cause**: `has_success_ci()` function returning false negatives
**Solution**: Fixed CI detection using check-runs API + duplicate prevention

**Test Fix**:
```bash
cd scripts/python/production
python -c "from monitor_pr_flow import has_success_ci; print(has_success_ci('ApprenticeGC/ithome-ironman-2025', '$BRANCH'))"
# Should return True for branches with successful CI
```

### Issue: Unicode Decoding Errors
**Symptoms**: `UnicodeDecodeError` in validation scripts
**Root Cause**: Binary API responses + UTF-8 decoding
**Solution**: Use `gh` CLI text commands instead of binary API

## 🔧 Key Scripts & Functions

### `monitor_pr_flow.py`
- **`has_success_ci()`**: Uses check-runs API to detect successful CI
- **`has_recent_ci_activity()`**: Prevents duplicate dispatches (10-min window)
- **Purpose**: Monitors Copilot PRs and ensures CI completion

### `direct_merge_pr.py`
- **`is_pr_eligible_for_merge()`**: Handles "unstable but mergeable" cases
- **`direct_merge_pr()`**: Bypasses GitHub auto-merge restrictions
- **Trigger**: `.github/workflows/direct-merge.yml` after approval success

### `validate_ci_logs.py`
- **Fixed**: UTF-8 handling using `gh run view --log` instead of binary API
- **Purpose**: Validates CI build logs for dotnet warnings/errors

### `ensure_automerge_or_comment.py`
- **Fixed**: GitHub API field names (`mergeStateStatus` vs invalid fields)
- **Purpose**: Enables auto-merge or adds status comments

## 🎯 Automation Flow Checkpoints

### 1. Issue → PR Creation
**Check**: Copilot branch exists and PR created
```bash
gh pr list --author=app/github-copilot --state=open
```

### 2. PR → CI Triggered
**Check**: CI workflow dispatched successfully
```bash
gh run list --branch=$BRANCH --limit=5 --json name,conclusion,status
```

### 3. CI → Auto-Ready
**Check**: `auto-ready-pr` workflow completed
```bash
gh run list --workflow=auto-ready-pr --limit=5 --json conclusion,headBranch
```

### 4. Ready → Auto-Merge Enabled
**Check**: Auto-merge status enabled on PR
```bash
gh pr view $PR_NUMBER --json autoMergeRequest,mergeable
```

### 5. Merge → Completion
**Check**: PR merged successfully
```bash
gh pr view $PR_NUMBER --json state,merged,mergedAt
```

## 🚦 Health Check Commands

### Quick Status Check
```bash
# Open Copilot PRs (should be 0 in healthy state)
gh pr list --author=app/github-copilot --state=open

# Recent automation workflows
gh run list --workflow=auto-approve-merge --limit=5 --json conclusion,headBranch,createdAt

# Direct merge usage frequency
gh run list --workflow=direct-merge --limit=10 --json conclusion,headBranch,createdAt
```

### Detailed Analysis
```bash
# Check for duplicate workflows pattern
BRANCH="copilot/fix-XXX"
gh api "repos/ApprenticeGC/ithome-ironman-2025/actions/runs?branch=$BRANCH&per_page=10" | jq '.workflow_runs[] | {name, created_at, conclusion}'

# Validate CI detection functions
cd scripts/python/production
python -c "
from monitor_pr_flow import has_success_ci, has_recent_ci_activity
branch = 'main'  # or specific branch
print(f'has_success_ci: {has_success_ci(\"ApprenticeGC/ithome-ironman-2025\", branch)}')
print(f'has_recent_ci_activity: {has_recent_ci_activity(\"ApprenticeGC/ithome-ironman-2025\", branch)}')
"
```

## 📊 Success Metrics

### Expected Behavior (Healthy State)
- ✅ **0 open Copilot PRs** (all processed and merged)
- ✅ **Single CI run per commit** (no duplicates)
- ✅ **<5 minute processing time** (ready to merged)
- ✅ **Direct merge as fallback only** (not primary path)

### Alert Conditions
- 🚨 **Multiple open Copilot PRs** for >30 minutes
- 🚨 **Duplicate build_test runs** on same commit
- 🚨 **High direct merge usage** (>50% of PRs)
- 🚨 **Unicode/API errors** in automation logs

## 💡 Pro Tips

1. **Always check commit check-runs** when debugging merge issues
2. **Use direct merge workflow** for "unstable but mergeable" PRs
3. **Monitor duplicate prevention** with `has_recent_ci_activity()`
4. **Validate API field names** when GitHub changes their schema
5. **Test CI detection functions** after any workflow changes

---
*For detailed technical analysis, see `docs/AUTOMATION_FLOW_ANALYSIS.md`*
