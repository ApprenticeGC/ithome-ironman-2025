# RFC Dependency Freeze – 2025-09-21

## Assignment Guard
- `assign-copilot-to-issue` workflow now skips when repository variable `COPILOT_ASSIGN_DISABLED` is set to `1`.
- Set that variable to pause new Copilot handoffs while dependency sequencing is defined.

## Current Queue Snapshot
(Generated via `gh issue list --state open`; bold items are currently assigned to Copilot.)

- GAME-RFC-014: **014-01** (#359), 014-02 (#360)
- GAME-RFC-013: **013-01** (#357), 013-02 (#358)
- GAME-RFC-012: **012-01** (#355), 012-02 (#356)
- GAME-RFC-011: **011-01** (#353), 011-02 (#354)
- GAME-RFC-010: **010-01** (#349), 010-02 (#350), 010-03 (#351), 010-04 (#352)
- GAME-RFC-009: **009-01** (#346), 009-02 (#347), 009-03 (#348)
- GAME-RFC-008: **008-01** (#344), 008-02 (#345)
- GAME-RFC-007: **007-01** (#342), 007-02 (#343)
- GAME-RFC-006: **006-01** (#340), 006-02 (#341)
- GAME-RFC-005: **005-01** (#338), 005-02 (#339)
- GAME-RFC-004: **004-01** (#335), 004-02 (#336), 004-03 (#337)
- GAME-RFC-003: **003-01** (#332), 003-02 (#333), 003-03 (#334)
- GAME-RFC-002: 002-03 (#331)

## Next Steps
1. Capture dependency metadata per micro RFC (e.g., add `DependsOn` in Notion).
2. Extend micro-issue generation and the mutex to respect cross-series dependencies before promoting queued work.
3. Re-enable assignments once dependency checks are in place.
