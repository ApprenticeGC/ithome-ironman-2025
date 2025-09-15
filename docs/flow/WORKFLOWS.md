# GitHub Workflows: Detailed Reference

Purpose: Definitive, practical guide to every workflow under `.github/workflows/` and the helper scripts they call. Use this to understand the end‑to‑end Copilot agent flow and avoid proposing duplicate automation.

Conventions
- Tokens: Assignment and GraphQL calls use a user PAT in `AUTO_APPROVE_TOKEN` when available; otherwise `GITHUB_TOKEN` (repo‑scoped). Assignment specifically prefers PAT to surface Copilot in `suggestedActors`.
- Copilot identity: Bot login `copilot-swe-agent`; assignment uses the GraphQL node id. Bot ids use `replaceActorsForAssignable`; user ids use `addAssigneesToAssignable`.

## Assignment and Sequencing

`assign-copilot-to-issue.yml`
- Trigger: `workflow_dispatch` (input: `issue_number`)
- Permissions: `contents: read`, `issues: write`
- Secrets: Uses `AUTO_APPROVE_TOKEN` if present; falls back to `GITHUB_TOKEN`
- What it does:
  - Prints `suggestedActors(capabilities:[CAN_BE_ASSIGNED])` for visibility.
  - Resolves issue node id and Copilot actor id:
    - Prefer Bot from `suggestedActors` (login contains "copilot").
    - Fallback to `user(login:"copilot-swe-agent")`.
  - Assigns:
    - Bot id → `replaceActorsForAssignable`
    - User id → `addAssigneesToAssignable`
- Outcome: Issue shows assignee "Copilot".

`assign-next-micro.yml` (alias; deprecated)
- Trigger: `workflow_dispatch` (input: `issue_number`)
- Same behavior as `assign-copilot-to-issue`; kept for backward compatibility.

`auto-advance-micro.yml`
- Trigger: `pull_request` (type `closed`) and `workflow_dispatch` (optional `pr_number`)
- Permissions: `contents: read`, `issues: write`, `pull-requests: read`; Environment: `copilot`
- Secrets: Requires `AUTO_APPROVE_TOKEN` (PAT) for assignment
- What it does:
  - For the merged PR (or specified PR), runs `.github/scripts/assign_next_micro_issue.py`.
  - The script derives RFC + micro index from PR body/title (Closes #N or RFC‑XXX‑YY).
  - Selection (script logic):
    1) Exact next token RFC‑XXX‑(YY+1)
    2) Earliest unassigned open micro with higher index
    3) Earliest unassigned open micro for same RFC (last resort)
  - Assigns Copilot using Bot/User mutation rules.
- Outcome: Next micro issue gets assigned to Copilot automatically.

`rfc-sync.yml`
- Trigger: `push` to `docs/game-rfcs/**.md`
- What it does:
  - Detects changed RFC MD files from the push payload.
  - For each, runs `generate_micro_issues_from_rfc.py` to create micros (assigns first to Copilot when available).
  - Then runs `assign_first_open_for_rfc.py` to ensure the earliest unassigned micro for that RFC is assigned.

`generate-micro-issues.yml`
- Trigger: `workflow_dispatch` (inputs: `rfc_path`, `assign_mode`)
- What it does:
  - Parses the RFC file sections/table, creates issues with titles like `RFC‑XXX‑YY: …`.
  - De‑dupes by exact title against open issues; assigns the first micro when possible.

## PR Lifecycle and CI

`ci.yml`
- Trigger: `pull_request` (all branches), and `push` to `copilot/**`
- What it does: `dotnet restore`, `dotnet build -warnaserror`, `dotnet test` for `./dotnet`.

`ensure-closes-link.yml`
- Trigger: `pull_request_target` on `opened`/`synchronize`
- What it does: For Copilot PRs with RFC in title, appends "Closes #<issue>" to body if missing.

`auto-ready-pr.yml`
- Triggers:
  - `workflow_run` for `ci` (on success, event `pull_request`)
  - `issue_comment` with `/ready` by a member/owner/collaborator
- What it does: Flips draft PR to "ready for review" after CI success, or when `/ready` is commented.

`auto-approve-merge.yml`
- Trigger: `pull_request_target` on `opened|ready_for_review|synchronize`
- Permissions: `contents: write`, `pull-requests: write`; Environment: `copilot`
- What it does:
  - Attempts to approve the PR (best effort) using `AUTO_APPROVE_TOKEN` or `GITHUB_TOKEN`.
  - Enables auto‑merge (squash). Requires repo setting "Allow auto‑merge".

## Recovery and Hygiene

`agent-watchdog.yml`
- Trigger: `workflow_run` for `ci` on `completed` with `conclusion=failure`
- What it does:
  - Derives failed run’s branch/PR/linked issue.
  - Runs `cleanup_recreate_issue.py` to close PR, delete branch, close linked issue, and recreate a new issue assigned to Copilot.
  - Adds a note when auto‑derivation is incomplete.

`cleanup-stalled-pr.yml`
- Trigger: `workflow_dispatch` (inputs: `pr_number`, optional `recreate_title`, `recreate_body`)
- What it does:
  - Derives head branch and linked issue from the PR body/title (close/fix/resolve #N).
  - Runs `cleanup_recreate_issue.py` to close PR, delete branch, close linked issue, and recreate a new issue assigned to Copilot.
- Use case: Manual reset of a stuck/conflicted PR chain without waiting for a failing CI run.

`cleanup-stalled-prs.yml`
- Trigger: `schedule` (every 6 hours) and `workflow_dispatch` (optional `max_age_hours`)
- What it does:
  - Automatically finds Copilot PRs that haven't been updated in 24+ hours.
  - For each stalled PR, extracts linked issue and runs `cleanup_recreate_issue.py` to reset the chain.
  - PRs without linked issues are simply closed and their branches deleted.
- Use case: Automated cleanup to prevent indefinitely stalled agent chains.

`rfc-dedupe.yml`
- Trigger: `schedule` daily and `workflow_dispatch`
- What it does: Runs `dedupe_rfc_issues.py` to detect and close duplicate RFC micro issues by title/token, linking to the canonical.

`rfc-assign-cron.yml`
- Trigger: scheduled (cron)
- What it does: Periodic maintenance to assign the next items (if configured; see workflow for specifics).

`runner-usage-badge.yml`
- Cosmetic: Generates runner usage badge info.

`loc-badge.yml`
- Trigger: scheduled every 6 hours; manual dispatch
- What it does: Counts C# LOC under `dotnet/game/**.cs` and publishes a badge artifact/commit on `main` only.

## Helper Scripts (referenced by workflows)

- `.github/scripts/assign_next_micro_issue.py`: Derives next issue from PR, selects candidate with fallbacks, assigns via GraphQL with Bot/User mutation choice.
- `.github/scripts/assign_first_open_for_rfc.py`: Chooses earliest unassigned micro for an RFC and assigns Copilot using GraphQL.
- `.github/scripts/generate_micro_issues_from_rfc.py`: Parses an RFC MD to create micro issues; de‑dupes by exact title; assigns first when possible.
- `.github/scripts/cleanup_recreate_issue.py`: Closes PR/branch/linked issue; recreates a new issue assigned to Copilot (Bot preferred) via GraphQL.
- `.github/scripts/dedupe_rfc_issues.py`: Groups issues by normalized RFC token and closes duplicates.

## End‑to‑End Flow (expected)

1) `generate-micro-issues` or `rfc-sync` creates micro issues; first is assigned to Copilot.
2) Copilot opens a draft PR; `ensure-closes-link` ensures Closes #N.
3) `ci` runs; on success `auto-ready-pr` flips to ready (or `/ready`).
4) `auto-approve-merge` enables auto‑merge; PR merges.
5) `auto-advance-micro` selects and assigns the next micro to Copilot.
6) If CI fails, `agent-watchdog` resets the chain; manual `cleanup-stalled-pr` is available.

Notes
- GITHUB_TOKEN must be Read+Write; assignments prefer `AUTO_APPROVE_TOKEN`.
- Copilot Coding Agent must be enabled; Bot should appear in `suggestedActors`.

