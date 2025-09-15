# RFC-090: Agent Flow – Operations Guide

Objective: Provide a quick-reference for operating and recovering the Game RFC Copilot flow.

Scope: Workflows under `.github/workflows`, helper scripts under `.github/scripts`, and the micro-issue lifecycle. Keep changes scoped to flow; do not alter Tier 1 contracts.

## Label Taxonomy (proposed)

- type:test-failure: Test failures observed in CI.
- type:infra: CI/workflow/script configuration issues.
- type:flaky: Non-deterministic failures needing stabilization.
- needs:repro: Needs reproduction steps or a minimal repro.
- needs:owner: Awaiting human owner assignment.
- flow:micro: Auto-generated micro issue from an RFC.
- flow:watchdog: Recreated by watchdog following CI failure.
- duplicate: Mark duplicates; link the canonical issue.

Notes:
- Prefer `type:*` for problem class, `needs:*` for next action, and `flow:*` for origin/context.
- Add `rfc:NNN` label (optional) for manual triage by RFC number.

## Core Workflows

- generate-micro-issues: Parses an RFC MD and creates micro issues; assigns first to Copilot when available.
- ci: Runs restore/build/test for PRs, and `copilot/**` branches.
- ensure-closes-link: Appends `Closes #<issue>` to Copilot PR bodies when missing.
- auto-ready-pr: Flips draft PR to ready after CI success; `/ready` comment also supported.
- auto-approve-merge: Approves and enables auto-merge for Copilot PRs (env `copilot`).
- auto-advance-micro: On PR merge, assigns the next micro issue to Copilot.
- agent-watchdog: On CI failure, closes PR/branch, recreates the micro issue, and reassigns Copilot.
- rfc-sync: On RFC MD changes, (re)generate micro issues and ensure the first is assigned.
- rfc-dedupe: Nightly/manual dedupe of duplicate issues; links to canonical and cleans up.
- assign-copilot-to-issue: Manual dispatch to assign a given issue to Copilot.

Naming note: Prefer `assign-copilot-to-issue`. `assign-next-micro` remains as a compatibility alias for now.

## Operator Commands (How-To)

- Generate from an RFC: Actions → `generate-micro-issues` → inputs `rfc_path`, `assign_mode=bot`.
- Manually assign: Actions → `assign-copilot-to-issue` → input `issue_number`.
- Manually advance after merge: Actions → `auto-advance-micro` → input optional `pr_number`.
- Recreate after failure: Actions → `rfc-dedupe` or let `agent-watchdog` auto-handle on `ci` failure.
- Flip PR to ready: Comment `/ready` on the PR (or wait for `auto-ready-pr`).

## Secrets and Permissions

- GITHUB_TOKEN: Provided by Actions; must allow `issues: write`, `pull-requests: write` per workflow.
- AUTO_APPROVE_TOKEN (optional): Environment `copilot`; used by `auto-approve-merge` to mitigate approval policy restrictions.

## Health Checks

1. Copilot bot presence: Repo `suggestedActors` must include a Copilot bot; otherwise assignment steps no-op.
2. CI matrix: Dotnet 8.0 is configured; ensure build/test pass locally.
3. Watchdog recycling: Confirm failed CI runs cause issue recreation; see workflow logs.
4. Dedupe: Trigger `rfc-dedupe` manually to consolidate duplicates if needed.

## Copilot Agent Identity

- Bot login observed via GraphQL `suggestedActors`: typically `copilot-swe-agent` with `__typename: Bot`.
- UI name appears as "Copilot"; assignment must use the Bot node `id` from GraphQL, not a username.
- Verify with:
  - `gh api graphql --raw-field query='query($owner:String!,$name:String!){ repository(owner:$owner,name:$name){ suggestedActors(capabilities:[CAN_BE_ASSIGNED],first:100){ nodes{ login __typename ... on Bot { id } } } } }' --raw-field owner='<owner>' --raw-field name='<repo>'`
- All flows here assign using `replaceActorsForAssignable` and the Copilot Bot `id`.

## Duplicates Handling

- Let `rfc-dedupe` close obvious duplicates and link to canonical. For edge cases, add `duplicate` label manually and comment with `Closes #<canonical>` on the PR.
- Avoid editing micro issue titles; the generator uses exact titles for idempotency.

## Change Management

- Keep changes scoped to flow; avoid renaming workflows unless also updating this doc and cross-references.
- Prefer additive changes (new inputs/steps) over disruptive renames.

## Appendix: Files of Interest

- .github/workflows/*.yml
- .github/scripts/*.py
