# RFC-093: Agent Flow Recovery (Track: game)

- Start Date: 2025-09-15
- RFC Author: Team
- Status: Draft
- Depends On: None
- Track: game

## Summary

Exercise the recovery path with doc-only changes so that cleanup/cron can be validated without breaking builds.

## Detailed Design

Documentation-only targets. If a PR stalls, use `cleanup-stalled-pr` to recreate the issue and let Copilot retry.

## Implementation Plan (Micro Issues)

| Micro | Title | Acceptance Criteria |
|-------|-------|---------------------|
| 01    | Create recovery placeholder | - [ ] Add `docs/game-rfc-test-93/PLACEHOLDER.md` with a one-line `ok-93` and an RFC reference; - [ ] Build passes; - [ ] PR title follows Conventional Commits |
| 02    | Adjust recovery placeholder | - [ ] Edit `docs/game-rfc-test-93/PLACEHOLDER.md` to include `retry-93`; - [ ] Build passes; - [ ] PR title follows Conventional Commits |
| 03    | Clean up recovery placeholder | - [ ] Remove `docs/game-rfc-test-93/PLACEHOLDER.md`; - [ ] Append a line in `AGENTS.md`: `Flow recovery tested: RFC-093`; - [ ] Build passes; - [ ] PR follows Conventional Commits |

