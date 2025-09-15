# RFC-090: End-to-End Agent Flow — Status & Plan

Objective: Fully automated loop for micro RFCs
- From issue → Copilot assignment → draft PR → CI → ready → auto‑merge → issue closed → next micro auto‑assigned.
- Self‑healing on failures via watchdog/reset; dedupe prevents backlog noise.

Current setup (as‑configured)
- Assignment: Uses PAT `AUTO_APPROVE_TOKEN`; resolves Copilot Bot via `suggestedActors` (Bot → `replaceActorsForAssignable`; User → `addAssigneesToAssignable`).
- Workflows: `generate-micro-issues`, `assign-copilot-to-issue`, `ci`, `ensure-closes-link`, `auto-ready-pr`, `auto-approve-merge`, `auto-advance-micro`, `agent-watchdog`, `cleanup-stalled-pr`, `rfc-sync`, `rfc-dedupe`.
- Policies: `GITHUB_TOKEN` is Read+Write; COPILOT agent enabled; LOC badge moved to 6‑hour schedule (no PR gating).

What we validated
- RFC‑090: Completed end‑to‑end (issues consumed, PRs merged, loop closed).
- RFC‑091: Verified sequential consumption after hardening (assignment via PAT, CI success, merge, auto‑advance; when selection missed, fallback logic now assigns earliest unassigned next micro).

Recent hardening
- Assignment reliability: Switched to PAT; Bot/User‑aware mutations; added runner `suggestedActors` debug (temporary).
- Auto‑advance selection: If exact next token not found, picks earliest unassigned with higher index, else earliest unassigned for same RFC; logs candidates when none.
- LOC badge: Removed PR triggers; scheduled every 6h only.

Observed gaps (and mitigations)
- Action approvals causing drafts to stall: Resolved by `GITHUB_TOKEN` Read+Write and using PAT for approvals/assignment.
- Auto‑advance “No next micro”: Addressed by selection fallback + candidate logging.
- Stalled/conflicted PRs: `agent-watchdog` resets on CI failure; `cleanup-stalled-pr` provides manual reset. Consider adding a scheduled sweep for stale Copilot PRs (optional).

Definition of Done for RFC‑090
- New micro issues are auto‑assigned to Copilot without manual steps.
- Copilot PRs run CI without approval prompts; `auto-ready-pr` flips to ready on success or via `/ready`.
- `auto-approve-merge` enables auto‑merge; merges close the linked issue; `auto-advance-micro` assigns the next micro consistently.
- Watchdog resets broken chains automatically; dedupe keeps the queue clean.

Outstanding items to fully “set and forget”
- Remove temporary `suggestedActors` debug steps in assignment workflows.
- Optional: Add a nightly/6h “stalled Copilot PR” sweep to auto‑reset PRs older than N hours with no progress.
- Optional: Tighten dedupe to normalize RFC tokens across minor title variants.

Operator quick actions (when needed)
- Reset a stuck chain: Actions → `cleanup-stalled-pr` → PR number.
- Trigger next micro manually: Actions → `Assign Copilot to Issue` → issue number.
- Generate from RFC: Actions → `generate-micro-issues` (assign_mode=bot).

Confidence
- With PAT + Bot‑id assignment and selection fallback, the flow is functionally complete. Remaining friction points are environmental (policy approvals) and rare edge cases (stalled PRs), both mitigated and optionally automatable.

