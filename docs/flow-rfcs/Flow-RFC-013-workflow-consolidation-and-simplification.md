# Flow-RFC-013: Workflow Consolidation and Simplification

## Status
In Progress (Phase 1 reusable workflows landed 2025-09-20)

## Problem
Overlapping workflows (multiple monitors, redundant CI triggers, partial duplication of approval logic) add maintenance overhead, increase chance of race conditions, and slow iteration.

## Goals
- Reduce workflow count while preserving functionality
- Centralize shared logic via `workflow_call` reusable workflows
- Clarify responsibility boundaries per workflow

## Inventory (Condensed)
- Monitoring: `auto-merge-monitor.yml`, `pr-flow-monitor.yml`
- Approval: `auto-approve-workflows.yml`, `auto-approve-merge.yml`
- CI: `ci.yml`, `ci-dispatch.yml`
- Cleanup: multiple variants

## Consolidation Plan
| Group | Current | Target |
|-------|---------|--------|
| Monitoring | 2 | 1 unified `flow-monitor.yml` |
| Approval | 2 | 1 `pr-approval.yml` (with conditional path) |
| CI | 2 | Keep `ci.yml`; convert dispatch to `workflow_call` entrypoint |
| Cleanup | Many | Keep `chain-health-scan.yml` + `stale-cleaner.yml` |

## Reusable Building Blocks
- `reusable-ci-dispatch.yml`: accepts `ref`, returns JSON outputs for status.
- `reusable-pr-merge.yml`: handles auto-merge enable + fallback decision.

## Script Adjustments
Refactor overlapping Python scripts into a single `orchestrator_cli.py` with subcommands:
- `monitor`
- `approve`
- `diagnose`
- `cleanup`

## Benefits
- Fewer cron schedules
- Easier reasoning about state transitions
- Shared caching & token broker integration (from RFC-011)

## Risk & Mitigation
| Risk | Mitigation |
|------|------------|
| Functional regression | Parallel run old + new for 1 week (shadow mode) |
| Hidden dependency loss | Dependency graph audit before removal |

## Test Plan
- Snapshot current workflow success rate baseline
- Deploy unified monitor in passive mode (logs only)
- Compare outputs for 7 days; if equivalent, disable legacy

## Acceptance Criteria
- Workflow count reduced ≥30%
- No increase in average issue→merge latency
- Zero increase in failure recovery time

## Rollout
Phase 1: implement reusable workflows
Phase 2: passive shadow
Phase 3: cutover + archive legacy.

## Implementation (Phase 1)
- Established `.github/workflows/reusable-ci-dispatch.yml` as the shared CI entrypoint and refactored `ci-dispatch.yml` to delegate via `workflow_call`.
- Added `.github/workflows/reusable-pr-merge.yml` for auto-merge enablement, with `auto-merge-monitor.yml` consuming it and running diagnostics via a follow-up job.
- Introduced `scripts/python/production/orchestrator_cli.py` with `monitor`, `approve`, `diagnose`, and `cleanup` subcommands; legacy workflows now route through the CLI for consistent execution paths.
- Updated monitors (`pr-flow-monitor.yml`, `auto-approve-workflows.yml`, `chain-health-scan.yml`) to call the orchestrator CLI, setting the stage for future consolidation into a single `flow-monitor.yml`.

## Implementation (Phase 2)
- Added `--shadow` support to the orchestrator CLI monitor command so workflows can execute in log-only mode without mutating repo state.
- Introduced `.github/workflows/flow-monitor-shadow.yml`, scheduled alongside the existing monitors to exercise the unified orchestrator in shadow mode for PR-flow and auto-merge scenarios.
- Shadow runs rely on the same secrets/environment wiring as production monitors, providing signal coverage ahead of the full cutover while keeping legacy automation active.

**Next**: compare shadow logs against legacy automation for a full week, then retire the standalone monitor workflows in favour of a consolidated `flow-monitor.yml`.
