# Flow-RFC-006: SQLite RFC Tracking Reliability Fix

## Status
Proposed (2025-09-19)

## Problem
The intended behavior of the RFC tracking SQLite database (`rfc_tracking.db`) is to persist Notion/file RFC processing state (page IDs, hashes, issue linkage) to prevent duplicate issue creation and detect content changes. Current observed behavior: records not reliably inserted/updated; change detection can re-create already processed issues or miss new tasks.

## Goals
- 100% reliable persistence of processed RFC units (Notion pages + derived micro-issues)
- Deterministic duplicate prevention across concurrent workflow runs
- Accurate change detection (no false negatives / positives beyond documented normalization rules)
- Idempotent re-runs (same input → zero side effects)

## Non-Goals
- Migrating to external DB (covered by future scalability RFC if needed)
- Full analytics layer

## Root Causes (Hypotheses)
1. Transaction boundaries not enforced (implicit autocommit causing race condition with artifact upload sequence)
2. Schema drift vs code expectations (columns missing / not created before use)
3. Concurrency: two workflow runs both download prior artifact, process, then re-upload last writer wins
4. Hash normalization inconsistencies (whitespace / ordering / Notion block types)
5. Error paths swallow exceptions before `commit()`
6. Lack of WAL mode leading to locking under GitHub Actions parallelism

## Proposed Solution
1. Schema Hardening
   - Add explicit migration step: checksum existing schema; create/alter tables if needed.
   - Maintain `schema_version` table.
2. Atomic Writer Pattern
   - Write to temp DB `rfc_tracking.tmp.db`; run integrity + expected row counts; atomically move to `rfc_tracking.db`.
3. Deterministic Hash Pipeline
   - Normalize content (lowercase? no; preserve semantic case) but collapse:
     - Trim trailing spaces
     - Normalize line endings to `\n`
     - Collapse >1 blank line to single
     - Remove Notion block decoration noise
   - Document hash algorithm: SHA-256 of UTF-8 bytes after normalization.
4. Concurrency Guard
   - Introduce ephemeral lock file `.rfc-db-lock` with PID + timestamp; stale after 5 min.
   - Workflows check & backoff (max 3 retries exponential 5s/10s/20s).
5. Observability
   - Add `processing_log` row per action with deterministic `trace_id` (uuid4).
   - Emit summary JSON artifact `rfc_processing_summary.json` (processed, skipped, changed, duplicates_prevented).
6. Integrity Verification Step
   - After run: query counts vs expectations; re-open DB read-only; compute PRAGMA integrity_check; fail if not `ok`.
7. Strict Error Policy
   - Any DB exception → exit non-zero with diagnostic dump.

## Data Model Adjustments
Add table `schema_version(version INTEGER PRIMARY KEY, applied_at TEXT)`.
Add index `idx_github_issues_page_id` on `github_issues(notion_page_id)`.
Add column `last_seen_hash` to `github_issues` for future historical diff (nullable initially).

## Algorithm (Pseudo)
```
load_or_create_db()
acquire_lock()
start transaction
for each source_page:
  normalized = normalize(content)
  hash = sha256(normalized)
  existing = SELECT * FROM notion_pages WHERE page_id=?
  if not existing:
     INSERT notion_pages(..., content_hash=hash)
     mark new
  else if existing.content_hash != hash:
     UPDATE notion_pages SET content_hash=?, updated_at=CURRENT_TIMESTAMP
     mark changed
  for each derived micro_issue:
     compute issue_hash (title + normalized_section)
     ensure no existing github_issues row with same hash
     create issue (unless dry-run) then INSERT github_issues
commit
integrity_check()
release_lock()
write summary artifact
```

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Lock file orphaned | TTL + stale lock override after 5m |
| Hash normalization regression | Unit tests with golden inputs |
| Parallel workflow override | Backoff + last-writer detection (compare artifact timestamp) |
| Migration failure mid-run | Execute in temp DB then move atomically |

## Test Plan
- Unit: normalization idempotency, hash stability.
- Integration: two simulated concurrent runs (force sleep) ensure no duplicate issue creation.
- Failure injection: corrupt DB → expect recreation + log.
- Golden test: same inputs across 3 runs → zero side effects after first.

## Metrics
Expose counts: `pages_new`, `pages_changed`, `issues_created`, `issues_skipped_duplicate`, `lock_wait_seconds`.

## Acceptance Criteria
- Duplicate issue creation across 5 consecutive runs: 0
- False change detection on unchanged content: 0 in test suite
- DB integrity_check always `ok`
- Concurrent double-run produces only one set of GitHub creations

## Rollout
1. Implement migration + normalization library
2. Add tests
3. Deploy behind env flag `RFC_DB_V2=1`
4. Monitor 3 days; remove old path

## Open Questions
- Should we embed a content diff snapshot for UX? (future)
- Need retention / pruning policy? (defer)
