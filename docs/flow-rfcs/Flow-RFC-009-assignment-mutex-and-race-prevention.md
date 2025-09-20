# Flow-RFC-009: Assignment Mutex and Race Prevention

## Status
In Progress (series lock enforcement merged 2025-09-19)

## Problem
Multiple issues in the same RFC series must not be simultaneously assigned to the automation bot. Current cron assignment logic reduces but does not eliminate race: concurrent workflow executions or manual + cron overlap can assign more than one micro issue.

## Goals
- Enforce SINGLE active assigned issue per RFC series
- Prevent concurrent assignment operations from conflicting
- Provide clear queueing for next issue candidates

## Non-Goals
- Reordering micro issue priorities based on complexity

## Design Overview
Introduce distributed soft-lock mechanism + pre-assignment validation.

### Lock Representation
- New GitHub issue label: `rfc-series-active:RFC-XYZ`
- Or alternative: dedicated hidden tracking issue (one per series) containing JSON state.
Chosen approach: **Tracking issue per RFC series** named: `RFC-XYZ Series State`.

### State Document Structure (in body fenced JSON)
```json
{
  "series": "RFC-093",
  "active_issue": 123,
  "queue": [124, 125],
  "updated_at": "2025-09-19T03:15:22Z",
  "version": 1
}
```

### Assignment Algorithm
```
resolve series from candidate issue title
fetch tracking issue (create if missing)
parse JSON; validate version
if active_issue open & assigned: abort (idempotent no-op)
set active_issue = candidate issue number; remove from queue if present
patch tracking issue body (ETag conditional update to avoid lost update)
assign bot
```

### Concurrency Control
- Use conditional update via `If-Match` header on issue update (GitHub REST ETag) or optimistic retry loop (max 5 tries).
- Add backoff jitter 200–800ms between retries.

### Queue Management
Unassigned issues discovered when scanning series get appended (dedupe). When active issue completes (closed by merged PR), next queue head is promoted by cron job.

### Failure Handling
- If active issue closed but PR not merged (abandoned), mark `active_issue=null` and promote next.
- If tracking issue missing or corrupted, rebuild from live issues (regenerate queue sorted by micro number).

## Observability
- Metrics JSON artifact: `assignment_mutex_metrics.json` with `promotion_events`, `duplicate_prevented`, `retries`, `conflicts`.
- Optional comment on tracking issue summarizing last promotion.

## Test Plan
Unit: parsing + queue operations, optimistic concurrency retries.
Integration: simulate two concurrent assignment attempts (pytest + threading or two processes) verifying only one success.
Recovery: corrupt tracking body → rebuild logic test.

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Tracking issue deleted | Recreate via discovery (label scan) |
| JSON corruption | Validate schema; fallback rebuild |
| Race still slips through | Optimistic loop + short cron interval (< conflict window) |

## Acceptance Criteria
- Zero double-assignment in 100 simulated concurrent attempts
- Active issue always reflects reality within one cron cycle

## Rollout
Phase 1: tracking issue generation + read-only metrics
Phase 2: enforce single assignment
Phase 3: integrate promotion events posting status comments.

## Implementation (Phase 1)
- Added `scripts/python/production/rfc_assignment_mutex.py` to manage per-series tracking issues and enforce single active issue assignment.
- Updated `assign-copilot-to-issue.yml` to acquire the lock before calling Copilot; the step aborts with `queued` status if another issue is active.
- State documents stored in `RFC-XYZ Series State` tracking issues include queued issue numbers for observability.

**Outstanding**: add automatic promotion/release hooks post-merge, optimistic concurrency via ETag, and queue-based auto-assignment in cron automation.
