# RFC-090: Agent Flow Smoke Test (Track: game)

- Start Date: 2025-09-15
- RFC Author: Team
- Status: Draft
- Depends On: None
- Track: game

## Summary

Create minimal, safe micro tasks to validate the end-to-end GitHub Copilot Coding Agent flow (issue → PR → CI → auto-advance/watchdog) without impacting runtime behavior.

## Motivation

We want a repeatable smoke test to ensure assignment, CI, auto-advance, and watchdog behavior work before applying the flow to larger features.

## Detailed Design

Tasks touch only documentation. CI remains green; no code paths change.

## Implementation Plan (Micro Issues)

| Micro | Title | Acceptance Criteria |
|-------|-------|---------------------|
| 01    | Create flow test placeholder | - [ ] Add `docs/game-rfc-test/PLACEHOLDER.md` with a one-line `ok` and a short note referencing this RFC; - [ ] Build passes with warnings-as-errors; - [ ] PR title uses Conventional Commits and links RFC-090 |
| 02    | Clean up flow test placeholder | - [ ] Remove `docs/game-rfc-test/PLACEHOLDER.md`; - [ ] Add a single-line note at end of `AGENTS.md` saying `Flow smoke tested: RFC-090`; - [ ] Build passes; - [ ] PR follows Conventional Commits |

## Notes

- Micro issues should be titled `RFC-090-01: Create flow test placeholder`, `RFC-090-02: Clean up flow test placeholder`.
- Assign to Copilot; let CI and auto-advance run. If CI fails, watchdog should recreate the issue and reassign.
