# Flow-RFC-008: Chain Consistency and Atomic Reset (Issue → PR → CI Runner → Branch)

## Status
In Progress (detection tooling merged 2025-09-19)

## Problem
Automation treats Issue, PR, CI runs, and branch as a logical chain. Failures (merge conflict, abandoned PR, missing branch, stuck CI) must trigger atomic teardown + recreation. Current implementation misses some edge cases:
- PR closed due to merge conflict or manual close leaves issue assigned and branch orphaned.
- Stale branches without open PR exist.
- Chains partially cleaned causing duplicate future attempts.

## Goals
- Deterministic detection of broken chain states
- Single atomic cleanup action (issue close or recreate, PR close/delete branch, cancel CI if running)
- Recreation logic only after successful cleanup preconditions verified
- Safety: never destroy unrelated branches or issues

## Chain Definition
`ChainID = RFC Series + Micro Number` (parsed from issue/PR title). All artifacts referencing that identifier are members.

## Broken State Conditions
| Code | Detection | Example |
|------|-----------|---------|
| BRANCH_ONLY | Branch exists, no PR, issue closed | leftover `copilot/rfc-093-02` |
| ISSUE_ONLY_ASSIGNED | Issue assigned, no PR after T threshold (e.g., 15m) | assignment stalled |
| PR_CLOSED_CONFLICT | PR closed with reason merge conflict | GitHub event webhook data |
| CI_STUCK | Last CI run > 30m pending | workflow dispatch hung |
| DUPLICATE_PRS | >1 open PR for same chain ID | race condition |

## Cleanup Algorithm (Pseudo)
```
identify all chain IDs (scan issues + PRs)
for each chain:
  classify state
  if healthy: continue
  log planned action
  if destructive mode enabled:
     cancel active workflows
     close PRs (comment with reason)
     delete branches (if pattern matches + merged status verified)
     close or recreate issue depending on policy
  if recreate_needed:
     create fresh issue (copy original body + increment revival counter)
     (optional) assign bot
```

## Recreation Policy
| Condition | Action |
|-----------|--------|
| PR_CLOSED_CONFLICT | Close PR, delete branch, reopen issue OR create new issue with suffix `-R1` |
| BRANCH_ONLY | Delete branch (safe heuristic: branch starts `copilot/`) |
| ISSUE_ONLY_ASSIGNED | Unassign (if possible) or close + recreate |
| DUPLICATE_PRS | Keep lowest PR number; close others + recreate their issues unassigned |

## Safety Mechanisms
- Dry-run mode prints JSON plan `chain_reset_plan.json`.
- Require env var `CHAIN_RESET_CONFIRM=1` to execute destructive phase in workflow.
- Branch deletion whitelist: regex `^(copilot|automation|bot)/`.
- Minimum age check (e.g., >5 min since creation) before automatic reset.

## Observability
- Emit metrics: `chains_total`, `chains_broken`, `chains_recreated`, `recreate_reason_counts`.
- Append machine-readable log `chain_actions.log` (JSON lines).

## Implementation Components
1. New script `chain_consistency_manager.py`.
2. Shared parsing utility for RFC identifiers (reusable by monitors).
3. Workflow `chain-health-scan.yml` (cron every 10m).
4. Optional manual `workflow_dispatch` with `destructive: true` input.

## Test Plan
- Synthetic fixtures: simulate each state class (mock GitHub API responses).
- End-to-end dry-run against current repo (should produce zero destructive actions initially).
- Induced conflict scenario via test branch rebase.

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Accidental deletion of user branch | strict naming + merged check + dry-run confirm |
| Flapping (recreate loop) | add `max_recreate_attempts` recorded in issue body hidden marker |
| API rate limits | batch queries (GraphQL) |

## Acceptance Criteria
- All five broken states classified correctly in tests
- Zero false positives on healthy chains across 50 sample evaluations
- Conflict-closed PR leads to clean recreation within one scan cycle

## Rollout
Phase 1: detection only (dry-run) → Phase 2: enable selective actions (conflict cases) → Phase 3: full automation.

## Implementation (Phase 1)
- Detection + planning script: `scripts/python/production/chain_consistency_manager.py` generates machine-readable remediation plans covering branch-only, issue-only-assigned, conflict-closed PRs, stuck CI runs, and duplicate PRs.
- Scheduled workflow: `.github/workflows/chain-health-scan.yml` runs every 10 minutes (and via `workflow_dispatch`) to publish `chain_reset_plan.json` artifacts.
- Unit coverage added under `scripts/python/tests/automation/test_chain_consistency.py` for identifier parsing and broken-state classification.

**Outstanding (Phase 2)**: implement `--destructive` cleanup actions (branch deletion, PR closure, issue recreation) with guard rails once the dry-run results are validated.
