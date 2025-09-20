# Notion Implementation RFC Ingestion (Flow-RFC-007 Phase 1)

This document explains the phase 1 reliable ingestion path for Implementation RFC pages from Notion.

## Goals (Phase 1)
- Deterministic discovery (external discovery script or explicit page IDs)
- Exactly-once creation semantics for NEW pages
- Skip unchanged pages efficiently
- Resilient to transient API failures (retry + backoff)
- Journaling for resumability

## Components
| File | Purpose |
|------|---------|
| `scripts/python/production/notion_reliability.py` | Reliable Notion client + ingestion orchestrator (`ingest_pages`) |
| `scripts/python/production/rfc_db_v2.py` | Durable RFC tracking DB (schema v2) |
| `scripts/python/production/generate_micro_issues_from_rfc.py` | Legacy micro issue generator (now can opt-in to reliable client) |

## Environment Flags
| Variable | Effect |
|----------|-------|
| `RFC_DB_V2=1` | Enables DB v2 integration in generation scripts |
| `NOTION_RELIABLE=1` | Uses retrying `NotionReliableClient` instead of basic client |

## Journaling
A JSONL journal (`notion_ingestion_journal.log` by default) records page processing outcome. Successful page hash entries allow subsequent runs to skip unchanged pages without a new issue.

Each line example:
```json
{"page_id": "abcd123", "hash": "<sha256>", "status": "SUCCESS"}
```

## Metrics
Summary metrics emitted to `notion_ingestion_metrics.json`:
- `pages_total`
- `pages_new`
- `pages_unchanged`
- `api_retries`
- `throttle_sleep_seconds`

## Workflow Integration
Current workflow (`.github/workflows/rfc-automation.yml`) calls the collection and per-page generation scripts. To adopt reliability phase 1 with minimal change:
1. Add env vars to relevant steps:
   ```yaml
   env:
     RFC_DB_V2: "1"
     NOTION_RELIABLE: "1"
   ```
2. (Optional) Add a preliminary ingestion dry-run step using `notion_reliability.ingest_pages` if you want bulk hashing without issue creation yet.

### Optional New Step Example
```yaml
- name: Reliable ingest (hash + journal)
  run: |
    python - <<'PY'
    from notion_reliability import ingest_pages
    import os
    pages = ["$PAGE1", "$PAGE2"]  # replace with discovery output
    ingest_pages(pages, db_path="./rfc_tracking.db", token=os.environ['NOTION_TOKEN'], dry_run=False)
    PY
  env:
    NOTION_TOKEN: ${{ secrets.NOTION_TOKEN }}
    RFC_DB_V2: "1"
```

## Readiness Assessment
Phase 1 is suitable for staging / dry-run in workflow now:
- NEW/UNCHANGED classification only (no issue commenting or updates yet)
- Safe (journal + DB) – unaffected steps can still fall back
- Does not remove legacy path; guarded by flags

## Next (Phase 2) Ideas
- UPDATED_MINOR/UPDATED_AFTER_CLOSE actions
- Diff summarization comments
- Bulk discovery integration replacing collection script

## Troubleshooting
| Symptom | Likely Cause | Mitigation |
|---------|--------------|------------|
| Many retries + slow run | Rate limiting | Inspect `api_retries`, consider lowering run frequency |
| Duplicate issue created | Page edited before hashing + legacy path bypassed | Ensure RFC_DB_V2 + NOTION_RELIABLE both set consistently |
| Journal not skipping | Hash changed due to metadata difference | Confirm last_edited/time/title stable; may relax extra fields in hash if noisy |

## Manual Dry Run
```bash
export NOTION_TOKEN=... RFC_DB_V2=1 NOTION_RELIABLE=1
python scripts/python/production/notion_reliability.py --pages <page_id> --dry-run
```
