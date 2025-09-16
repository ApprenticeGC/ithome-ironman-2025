# RFC-090: End-to-End Flow Smoke Test Runbook

Purpose: Validate the Copilot agent flow from micro-issue generation → PR → CI → auto-ready → auto-merge → auto-advance.

Pre-checks
- Repo access: You are logged in via `gh auth status` with repo write.
- Secrets: `AUTO_APPROVE_TOKEN` present in repo or environment `copilot` (verified earlier). `GITHUB_TOKEN` is provided by Actions.
- CI: Local `dotnet build ./dotnet -warnaserror` and `dotnet test ./dotnet --no-build` succeed, or you intentionally test watchdog behavior.
- Copilot bot: Repo environments show `copilot`; suggestedActors include a Copilot bot (assignment steps depend on it).

Test Steps
1) Generate micro issues from an RFC
   - GitHub UI: Actions → `generate-micro-issues` → inputs:
     - `rfc_path`: path under `docs/game-rfcs/` (e.g., `docs/game-rfcs/RFC-090-agent-flow-smoke-test.md`).
     - `assign_mode`: `bot`.
   - Expected: Workflow logs show created issues; first micro assigned to Copilot.

2) Watch assignment and PR creation
   - Copilot picks up the first micro, opens a draft PR on branch `copilot/rfc-XXX-YY-*`.
   - `ensure-closes-link` appends `Closes #<issue>` to the PR body if missing.

3) CI and ready-for-review
   - `ci` runs restore/build/test for PR.
   - On success, `auto-ready-pr` flips draft to ready. Alternatively, comment `/ready` on the PR.

4) Approve and auto-merge
   - `auto-approve-merge` approves the PR and enables squash auto-merge (requires repo setting Allow auto-merge).
   - On merge, the branch is removed by GitHub (default), PR is closed.

5) Auto-advance to next micro
   - `auto-advance-micro` detects the merged PR and assigns the next micro issue to Copilot.
   - Expected: The next `RFC-XXX-YY+1` issue becomes assigned to Copilot.

6) Failure path (optional)
   - Intentionally break a test or build to trigger `ci` failure.
   - `agent-watchdog` reacts to the failed `ci` run: closes PR/branch, recreates the micro issue, labels as needed, reassigns Copilot.
   - Expected: A fresh issue is created with a note referencing the failed run.

Troubleshooting
- No Copilot assignment: Ensure the Copilot bot shows up under `suggestedActors` and that the org/repo has Copilot enabled.
- Auto-merge not enabled: Add `AUTO_APPROVE_TOKEN` or enable Actions approvals, and ensure repo Allows auto-merge.
- Duplicates: Run `rfc-dedupe` from Actions to consolidate duplicate issues.

Rollback / Cleanup
- If a PR is stuck, comment `/ready` or close it; `agent-watchdog` will recycle on next failed `ci` or run `rfc-dedupe` to reconcile issues.
