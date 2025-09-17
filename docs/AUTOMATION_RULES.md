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

## WHY THIS RULE EXISTS:

1. **Goal**: True end-to-end automation from issue → PR → merge
2. **Problem**: Manual intervention defeats the automation purpose
3. **Evidence**: PR #96, #98 appeared "automated" but were actually manually merged
4. **Learning**: If workflows exist but PRs don't merge, **the workflows are broken** - not automation gaps

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
