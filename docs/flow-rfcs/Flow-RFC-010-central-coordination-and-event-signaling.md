# Flow-RFC-010: Central Coordination and Event Signaling Layer

## Status
In Progress (event router + emitter added 2025-09-20)

## Problem
Workflows currently operate via polling (cron + broad triggers). Consequences: latency, duplicate processing, wasted minutes, and races. No shared event bus or state aggregator.

## Goals
- Reduce reliance on cron schedules
- Provide low-latency signaling between lifecycle stages (issue assigned → ingestion; PR ready → merge monitor)
- Standardize event payload schema

## Non-Goals
- External infrastructure (Kafka, SQS) – must remain GitHub-native

## Design
Use `repository_dispatch` + structured JSON payloads as an internal event bus.

### Event Types
| Event | Producer | Consumer(s) | Payload Core |
|-------|----------|-------------|--------------|
| `rfc.issue.assigned` | assignment workflow | PR creator monitor | `{ "issue": 123, "series": "RFC-093", "micro": "02" }` |
| `rfc.pr.created` | PR creation detector | CI dispatcher, merge orchestrator | `{ "pr": 456, "issue": 123, "branch": "copilot/rfc-093-02" }` |
| `rfc.pr.ready` | readiness evaluator | merge orchestrator | `{ "pr":456, "checks_status":"green" }` |
| `rfc.pr.blocked` | diagnostics | human / auto-remediator | `{ "pr":456, "blockers":[...] }` |
| `rfc.chain.broken` | chain consistency (RFC-008) | chain reset | `{ "chain_id":"RFC-093-02", "reason":"PR_CLOSED_CONFLICT" }` |

### Implementation Components
1. Event Emitter Helper (Python): `emit_event(event_type, payload)` → calls GitHub REST `POST /repos/:owner/:repo/dispatches`.
2. Workflow `event-router.yml` listening on `repository_dispatch` and conditionally invoking other workflows (via `workflow_call`).
3. Schema Validation: JSON Schema definitions in `docs/events/schema/` validated in CI (ajv CLI container step).
4. Deduplication: Include `event_id` (UUIDv4) + `source` + timestamp; maintain short-lived in-memory (or issue comment) cache to drop duplicates.

### Migration Strategy
- Keep cron jobs initially but add event emission.
- Once reliability proven, increase cron interval or remove redundant ones.

## Security / Abuse Considerations
- Only workflows / internal scripts emit events (using GITHUB_TOKEN scope limited to repo).
- Validate `client_payload.event_id` length & pattern.

## Observability
- Event log artifact `event_log.jsonl` appended by router.
- Metrics: counts per event_type, median routing latency.

## Test Plan
- Unit: schema validation for each event type
- Integration: end-to-end dispatch from assignment to merge orchestrator
- Failure injection: malformed payload rejected gracefully with logged error

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Event loss (rate limits) | Retry with jitter, fallback cron path remains |
| Duplicate events | event_id caching (60m TTL) |
| Payload drift | JSON schema enforced in CI |

## Acceptance Criteria
- 50% reduction in average time from PR readiness to merge attempt
- No duplicate processing events in test harness
- All events validated against schema; zero schema drift errors over 7 days

## Rollout
Phase 1: implement emitter + router (log only)
Phase 2: route to CI + merge monitor
Phase 3: deprecate selected cron workflows.

## Implementation (Phase 1)
- Added `scripts/python/production/event_bus.py` with `emit_event` helper (and CLI) for repository_dispatch triggers.
- New workflow `.github/workflows/event-router.yml` validates payloads against `docs/events/schema/` and records them to `event_log.jsonl`.
- Assignment workflow emits `rfc.issue.assigned` events; chain health scan emits `rfc.chain.broken` when inconsistencies are detected.

## Implementation (Phase 2)
- Event router now dispatches downstream automation:
  - `rfc.issue.assigned` immediately triggers `pr-flow-monitor.yml` to kick the PR creation + CI readiness checks without waiting for the 5-minute cron.
  - `rfc.chain.broken` invokes targeted cleanup workflows, running `rfc-cleanup-duplicates.yml` for duplicate/conflict states and `cleanup-stalled-prs.yml` for branch-only / CI-stuck states.
- Router permissions elevated to `actions: write` so it can call `gh workflow run` safely with the repo token.
- Phase 2 keeps logging artifacts for observability while reducing end-to-end latency by running the same remediation flows proactively.
