# 🚫 CRITICAL AUTOMATION RULE 🚫

## NO MANUAL PR MERGING ALLOWED

**This is a STRICT rule that must be followed at ALL times:**

### ❌ NEVER DO:
- `gh pr merge <number>`
- `python monitor_pr_flow.py` (manual script execution)
- `python auto_approve_or_dispatch.py` (manual script execution)
- `python direct_merge_pr.py` (manual script execution)
- Any manual GitHub UI merging
- Any manual script execution that results in PR merging

### ✅ ALWAYS DO:
- Fix the **automated workflows** so they work properly
- Let **GitHub Actions workflows** handle everything automatically
- Debug **workflow chains** when they're broken
- Fix **workflow approval issues** through automation
- Use **repository settings** and **workflow configuration** to enable automation

## ✅ SUCCESS EVIDENCE:

**Latest Achievements** (September 17, 2025):
- **PR #100**: ✅ Automatically merged at `2025-09-17T06:45:11Z` - RFC-002 implementation
- **PR #98**: ✅ Automatically merged - RFC-097-01 Unicode validation
- **PR #96**: ✅ Automatically merged - RFC-092-03 CI validation
- **PR #95**: ✅ Automatically merged - RFC-092-02 PR creation
- **PR #94**: ✅ Automatically merged - RFC-092-01 assignment validation

**Zero Manual Interventions Required**: Complete automation pipeline operational.

## WHY THIS RULE EXISTS:

1. **Goal**: True end-to-end automation from issue → PR → merge ✅ **ACHIEVED**
2. **Problem**: Manual intervention defeats the automation purpose
3. **Evidence**: Previous manual merges prevented automation validation
4. **Learning**: When workflows exist but PRs don't merge, **the workflows are broken** - not automation gaps
5. **Result**: Fixed workflows now handle 100% of Copilot PRs automatically

## WHEN TEMPTED TO MERGE MANUALLY:

Instead of merging manually, ask:
- "Why isn't the existing workflow working?"
- "What's blocking the automation chain?"
- "How can I fix the workflow to handle this automatically?"

## DEBUGGING PROCESS:

1. **Identify the workflow chain** (e.g., auto-approve-merge → direct-merge)
2. **Check workflow status** (`gh run list --workflow=<name>`)
3. **Find bottlenecks** (action_required, failures, missing triggers)
4. **Fix the workflow configuration** or scripts
5. **Test the automated flow** without manual intervention

## SUCCESS METRICS:

- PR merges happen **without any manual commands**
- Workflow runs show **successful completion chain**
- **Zero manual GitHub CLI or UI interactions** for merging
- **Complete automation visibility** through workflow logs

---

**Remember: If you manually merge, you've broken the automation goal. Fix workflows instead!** 🤖
