# Flow-RFC-007: Reliable Notion Implementation RFC Ingestion

## Status
Proposed (2025-09-19)

## Problem
Implementation RFC pages in Notion must map 1:1 to GitHub issues without omission or duplication. Current ingestion occasionally:
- Misses newly edited pages (change not detected)
- Recreates existing issues if state artifacts lost
- Processes pages partially (network / rate limit interruptions)

## Goals
- Deterministic discovery of all relevant Notion implementation RFC pages
- Exactly-once issue creation semantics per page-task unit
- Incremental change detection (reflect edits by creating follow-up issues or updating description—decision below)
- Resumable processing after transient failure

## Scope
Implementation RFC pages only (excludes architectural high-level docs). Assumes Flow-RFC-006 DB reliability improvements.

## Functional Requirements
1. Discovery Modes: single page, list, collection root crawl.
2. Pagination Handling: robust for >100 blocks.
3. Retry Logic: exponential backoff (HTTP 429, 5xx) with jitter; max 5 attempts.
4. Consistent Mapping: Notion page ID → canonical RFC identifier → issue title.
5. Dry-run mode prints planned actions + change classification.
6. Error Classification: permanent vs transient; only transient retried.
7. Partial Progress Persistence: each processed page checkpointed before moving on.

## Non-Goals
- Bi-directional syncing back into Notion status fields (future RFC)
- Advanced diff summarization (future enhancement)

## Design
### Discovery
- Use `--notion-collection` to list child pages via Notion children API recursively (depth 2 limit initially).
- Filter by title regex: `^Game-RFC-[0-9]{3}-[0-9]{2}`.

### Page Normalization
- Extract blocks → flatten into markdown-like text preserving bullet hierarchy.
- Strip decorative elements (divider, unsupported, toggles expanded inline).
- Normalize whitespace prior to hashing (shared with RFC-006).

### Change Classification
| Condition | Classification | Action |
|-----------|---------------|--------|
| No prior record | NEW | Create issue |
| Hash same | UNCHANGED | Skip |
| Hash changed & issue open | UPDATED_MINOR | Comment on issue with summary diff (optional phase 2) |
| Hash changed & issue closed | UPDATED_AFTER_CLOSE | Create follow-up issue (suffix `-REV1`) |

(Phase 1: implement NEW/UNCHANGED only; record changes for later follow-up.)

### Resumability
- After each page: commit DB + append JSON line to `notion_ingestion_journal.log` with status.
- On restart: skip pages with journal status `SUCCESS` and matching stored hash.

### API Reliability
- Shared Notion client with rate limiter (token bucket: 3 req/sec sustained, burst 5) to avoid hitting global limits.

### Configuration
Env vars: `NOTION_TOKEN` (required), `NOTION_TIMEOUT` (default 30s), `NOTION_RETRIES` (default 5).
CLI flags: `--resume`, `--journal-path`, `--collection`, `--page`, `--force`, `--emit-diff`(future).

## Data Additions
Add table `notion_processing_journal(id INTEGER PK, page_id TEXT, status TEXT, hash TEXT, processed_at TEXT)`. Index on `(page_id, hash)`.

## Metrics
- `pages_total`, `pages_new`, `pages_unchanged`, `pages_changed`, `api_retries`, `throttle_sleep_seconds`.

## Error Handling
- Transient (429/5xx/network): retry with backoff
- Permanent (403 invalid token, 404 missing): mark FAILED_PERM and continue
- At end: non-zero exit if any NEW failures occurred

## Test Plan
- Mock Notion API tests (fixture JSON) for discovery + normalization.
- Live smoke test with 2 sample pages (manual token) behind `--live-test` flag (skipped in CI by default).
- Journal resumability test: simulate crash mid-run.

## Acceptance Criteria
- Re-running ingestion on unchanged collection results in 0 API calls for unchanged pages after initial HEAD listing.
- No duplicate issues after 5 consecutive runs.
- Missed edit scenario captured in journal and hash updated.

## Rollout
1. Implement discovery + hashing (reuse normalization lib from RFC-006)
2. Integrate with DB
3. Add metrics export JSON
4. Dry-run validation with existing pages
5. Enable in workflow replacing current ad-hoc ingestion

## Open Questions
- Should changed content auto-update existing issue body? (defer for policy)
- Do we close stale issues if page deleted? (future clean-up RFC)
