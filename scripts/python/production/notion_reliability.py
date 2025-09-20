"""Reliable Notion ingestion utilities per Flow-RFC-007 (phase 1: NEW/UNCHANGED).

Features implemented:
- Token bucket rate limiting (3 rps sustained, burst 5)
- Retry with exponential backoff + jitter for transient errors (429, 5xx, network)
- Error classification transient vs permanent
- Page content normalization + hashing (reuse rfc_db_v2.normalize_content/stable_hash)
- Journaling (file JSONL + DB table notion_processing_journal)
- Dry-run planning output
- Metrics emission via rfc_db_v2.emit_summary

Phase 1 classification: NEW vs UNCHANGED only.
"""

from __future__ import annotations

import json
import os
import random
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Tuple

from rfc_db_v2 import PageRecord, emit_summary, normalize_content, open_db, stable_hash

DEFAULT_RETRIES = int(os.environ.get("NOTION_RETRIES", "5"))
DEFAULT_TIMEOUT = int(os.environ.get("NOTION_TIMEOUT", "30"))
RATE_PER_SEC = 3
BURST = 5

TRANSIENT_STATUS = {429, 500, 502, 503, 504}


@dataclass
class PageState:
    page_id: str
    title: str
    last_edited: str
    content: str
    content_hash: str


class TokenBucket:
    def __init__(self, rate: float, burst: int):
        self.rate = rate
        self.capacity = burst
        self.tokens = burst
        self.timestamp = time.time()

    def consume(self, n: int = 1):
        while True:
            now = time.time()
            # replenish
            delta = now - self.timestamp
            self.timestamp = now
            self.tokens = min(self.capacity, self.tokens + delta * self.rate)
            if self.tokens >= n:
                self.tokens -= n
                return
            sleep_for = (n - self.tokens) / self.rate
            time.sleep(min(sleep_for, 1.0))


class NotionReliableClient:
    def __init__(self, token: str, *, retries: int = DEFAULT_RETRIES, timeout: int = DEFAULT_TIMEOUT):
        self.token = token
        self.retries = retries
        self.timeout = timeout
        self.base_url = "https://api.notion.com/v1"
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Notion-Version": "2022-06-28",
            "Content-Type": "application/json",
        }
        self.bucket = TokenBucket(RATE_PER_SEC, BURST)
        self.metrics = {
            "api_retries": 0,
            "throttle_sleep_seconds": 0.0,
            "pages_total": 0,
            "pages_new": 0,
            "pages_unchanged": 0,
        }

    # ---- low-level request ----
    def _request(self, path: str) -> Dict[str, Any]:
        url = f"{self.base_url}{path}"
        attempt = 0
        while True:
            self.bucket.consume()
            req = urllib.request.Request(url, headers=self.headers)
            try:
                with urllib.request.urlopen(req, timeout=self.timeout) as resp:
                    return json.loads(resp.read().decode())
            except urllib.error.HTTPError as e:
                status = e.code
                body = e.read().decode() if hasattr(e, "read") else ""
                if status in TRANSIENT_STATUS and attempt < self.retries - 1:
                    attempt += 1
                    backoff = (2**attempt) + random.uniform(0, 0.25)
                    self.metrics["api_retries"] += 1
                    self.metrics["throttle_sleep_seconds"] += backoff
                    time.sleep(backoff)
                    continue
                if status in (403, 404):
                    raise PermanentNotionError(f"Permanent HTTP {status}: {body[:200]}")
                raise TransientNotionError(f"Unhandled HTTP {status}: {body[:200]}")
            except (urllib.error.URLError, TimeoutError) as e:
                if attempt < self.retries - 1:
                    attempt += 1
                    backoff = (2**attempt) + random.uniform(0, 0.25)
                    self.metrics["api_retries"] += 1
                    self.metrics["throttle_sleep_seconds"] += backoff
                    time.sleep(backoff)
                    continue
                raise TransientNotionError(f"Network error: {e}")

    # ---- API wrappers ----
    def get_page(self, page_id: str) -> Dict[str, Any]:
        return self._request(f"/pages/{page_id}")

    def get_block_children(self, block_id: str, *, page_size: int = 100) -> List[Dict[str, Any]]:
        results: List[Dict[str, Any]] = []
        start_cursor: Optional[str] = None
        while True:
            q = f"/blocks/{block_id}/children?page_size={page_size}"
            if start_cursor:
                q += f"&start_cursor={start_cursor}"
            data = self._request(q)
            results.extend(data.get("results", []))
            if not data.get("has_more"):
                break
            start_cursor = data.get("next_cursor")
        return results

    # ---- Normalization / hashing ----
    def flatten_blocks(self, blocks: List[Dict[str, Any]]) -> str:
        parts: List[str] = []
        for b in blocks:
            t = b.get("type")
            if t == "heading_3":
                txt = self._rich_text(b["heading_3"].get("rich_text", []))
                parts.append(f"### {txt}")
            elif t == "paragraph":
                txt = self._rich_text(b["paragraph"].get("rich_text", []))
                if txt.strip():
                    parts.append(txt)
            elif t == "bulleted_list_item":
                txt = self._rich_text(b["bulleted_list_item"].get("rich_text", []))
                parts.append(f"- {txt}")
            elif t == "to_do":
                td = b["to_do"]
                txt = self._rich_text(td.get("rich_text", []))
                checked = "x" if td.get("checked") else " "
                parts.append(f"- [{checked}] {txt}")
            # skip decorative types for phase 1
        return normalize_content("\n".join(parts))

    def _rich_text(self, arr: List[Dict[str, Any]]) -> str:
        return "".join(i.get("plain_text", "") for i in arr)

    # ---- Page pipeline ----
    def fetch_page_state(self, page_id: str) -> PageState:
        meta = self.get_page(page_id)
        title_prop = meta.get("properties", {}).get("title", {})
        title = (
            "".join(i.get("plain_text", "") for i in title_prop.get("title", []))
            if title_prop.get("type") == "title"
            else page_id
        )
        last_edited_time = meta.get("last_edited_time", "")
        blocks = self.get_block_children(page_id)
        content = self.flatten_blocks(blocks)
        h = stable_hash(content, extra={"last_edited_time": last_edited_time, "title": title})
        return PageState(page_id=page_id, title=title, last_edited=last_edited_time, content=content, content_hash=h)


# ---- Exceptions ----
class PermanentNotionError(RuntimeError):
    pass


class TransientNotionError(RuntimeError):
    pass


# ---- Journaling helpers ----


def append_journal_line(path: Path, entry: Dict[str, Any]):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(entry, sort_keys=True) + "\n")


def load_journal_index(path: Path) -> Dict[Tuple[str, str], str]:
    idx: Dict[Tuple[str, str], str] = {}
    if not path.exists():
        return idx
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        try:
            rec = json.loads(line)
            idx[(rec.get("page_id"), rec.get("hash"))] = rec.get("status", "")
        except Exception:
            continue
    return idx


# ---- Ingestion orchestrator (phase 1) ----


def ingest_pages(
    page_ids: Iterable[str],
    *,
    db_path: str,
    token: str,
    dry_run: bool = False,
    journal_path: str = "notion_ingestion_journal.log",
):
    client = NotionReliableClient(token)
    journal_file = Path(journal_path)
    journal_index = load_journal_index(journal_file)

    with open_db(db_path) as db:
        pages_total = 0
        new_count = 0
        unchanged_count = 0
        for pid in page_ids:
            pages_total += 1
            try:
                state = client.fetch_page_state(pid)
            except PermanentNotionError as e:
                append_journal_line(journal_file, {"page_id": pid, "status": "FAILED_PERM", "error": str(e)})
                continue
            except TransientNotionError as e:
                append_journal_line(journal_file, {"page_id": pid, "status": "FAILED_TRANSIENT", "error": str(e)})
                continue

            # Journal skip check
            if (pid, state.content_hash) in journal_index and journal_index[(pid, state.content_hash)] == "SUCCESS":
                unchanged_count += 1
                if not dry_run:
                    append_journal_line(
                        journal_file, {"page_id": pid, "hash": state.content_hash, "status": "UNCHANGED"}
                    )
                else:
                    print(f"DRY-RUN: SKIP {pid} unchanged")
                continue

            # Store page (phase 1 treat as NEW always if not skipped)
            if dry_run:
                print(f"DRY-RUN: NEW {pid} -> issue would be created")
                new_count += 1
                continue
            # Persist page in DB (issue creation outside scope here)
            db.upsert_page(
                PageRecord(
                    page_id=pid,
                    page_title=state.title,
                    last_edited_time=state.last_edited,
                    content_hash=state.content_hash,
                    rfc_identifier=pid,
                )
            )
            append_journal_line(journal_file, {"page_id": pid, "hash": state.content_hash, "status": "SUCCESS"})
            new_count += 1

        client.metrics["pages_total"] = pages_total
        client.metrics["pages_new"] = new_count
        client.metrics["pages_unchanged"] = unchanged_count
        emit_summary("notion_ingestion_metrics.json", **client.metrics)


# CLI for manual runs
if __name__ == "__main__":
    import argparse

    ap = argparse.ArgumentParser(description="Ingest Notion implementation RFC pages (phase 1)")
    ap.add_argument("--pages", nargs="*", help="Explicit page IDs to ingest")
    ap.add_argument("--db", default="rfc_tracking.db")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--journal", default="notion_ingestion_journal.log")
    args = ap.parse_args()
    token = os.environ.get("NOTION_TOKEN")
    if not token:
        print("Missing NOTION_TOKEN", file=sys.stderr)
        sys.exit(2)
    ingest_pages(args.pages, db_path=args.db, token=token, dry_run=args.dry_run, journal_path=args.journal)
