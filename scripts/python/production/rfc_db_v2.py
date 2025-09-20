"""RFC DB V2 reliability layer per Flow-RFC-006.

Features:
- Schema versioning & migrations
- Content normalization + hashing
- Atomic write pattern (temp DB promotion)
- File lock with stale lock recovery
- Summary emission helper
- Context manager interface

Activation: set environment variable RFC_DB_V2=1 in workflows.
"""

from __future__ import annotations

import contextlib
import dataclasses
import hashlib
import json
import os
import shutil
import sqlite3
import tempfile
import time
from pathlib import Path
from typing import Any, Dict, Optional

SCHEMA_VERSION = 2

SCHEMA_STMTS = [
    """
    CREATE TABLE IF NOT EXISTS schema_version(
        version INTEGER PRIMARY KEY,
        applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS notion_pages (
        page_id TEXT PRIMARY KEY,
        page_title TEXT NOT NULL,
        last_edited_time TEXT NOT NULL,
        content_hash TEXT NOT NULL,
        rfc_identifier TEXT NOT NULL,
        status TEXT DEFAULT 'active',
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS github_issues (
        issue_number INTEGER PRIMARY KEY,
        issue_title TEXT NOT NULL,
        issue_state TEXT NOT NULL,
        notion_page_id TEXT,
        content_hash TEXT NOT NULL,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """,
    """
    CREATE TABLE IF NOT EXISTS processing_log (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        notion_page_id TEXT NOT NULL,
        action TEXT NOT NULL,
        details TEXT,
        trace_id TEXT,
        timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    """,
    """
    CREATE INDEX IF NOT EXISTS idx_github_issues_page_id ON github_issues(notion_page_id);
    """,
    """
    CREATE TABLE IF NOT EXISTS notion_processing_journal (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        page_id TEXT NOT NULL,
        status TEXT NOT NULL,
        hash TEXT,
        processed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        UNIQUE(page_id, hash, status)
    );
    """,
]

LOCK_FILENAME = ".rfc-db-lock"
LOCK_STALE_SECONDS = 300


def _now_ts() -> float:
    return time.time()


def normalize_content(raw: str) -> str:
    """Deterministic normalization prior to hashing."""
    # Line ending normalization
    text = raw.replace("\r\n", "\n").replace("\r", "\n")
    # Collapse >1 blank lines
    lines = []
    blank = 0
    for line in text.split("\n"):
        if line.strip() == "":
            blank += 1
            if blank > 1:
                continue
        else:
            blank = 0
        # Trim trailing spaces
        lines.append(line.rstrip())
    # Remove trailing blank lines
    while lines and lines[-1].strip() == "":
        lines.pop()
    return "\n".join(lines).strip()


def stable_hash(content: str, *, extra: Optional[Dict[str, Any]] = None) -> str:
    payload = {"content": normalize_content(content)}
    if extra:
        payload.update(extra)
    blob = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(blob).hexdigest()


@dataclasses.dataclass
class PageRecord:
    page_id: str
    page_title: str
    last_edited_time: str
    content_hash: str
    rfc_identifier: str
    status: str = "active"


class RfcDb:
    def __init__(self, path: str):
        self.original_path = Path(path)
        self.tmp_dir = Path(tempfile.mkdtemp(prefix="rfcdbv2-"))
        self.tmp_path = self.tmp_dir / "rfc_tracking.tmp.db"
        # Copy existing DB if present
        if self.original_path.exists():
            shutil.copy2(self.original_path, self.tmp_path)
        # Use default transactional behavior (DEFERRED) so BEGIN/COMMIT work as expected
        self.conn = sqlite3.connect(self.tmp_path, isolation_level="DEFERRED")
        self._migrate()

    # ---- Migration ----
    def _migrate(self):
        cur = self.conn.cursor()
        for stmt in SCHEMA_STMTS:
            cur.executescript(stmt)
        v = cur.execute("SELECT MAX(version) FROM schema_version").fetchone()[0]
        if v is None or v < SCHEMA_VERSION:
            cur.execute("INSERT INTO schema_version(version) VALUES (?)", (SCHEMA_VERSION,))
        self.conn.commit()

    # ---- Locking ----
    def acquire_lock(self):
        lock_file = self.original_path.parent / LOCK_FILENAME
        while True:
            if lock_file.exists():
                try:
                    data = json.loads(lock_file.read_text())
                    ts = data.get("ts", 0)
                    if _now_ts() - ts > LOCK_STALE_SECONDS:
                        # stale lock
                        lock_file.unlink(missing_ok=True)
                    else:
                        time.sleep(2)
                        continue
                except Exception:
                    lock_file.unlink(missing_ok=True)
            # create
            try:
                lock_file.write_text(json.dumps({"ts": _now_ts(), "pid": os.getpid()}))
                self._lock_file = lock_file
                return
            except Exception:
                time.sleep(1)

    def refresh_lock(self):
        if hasattr(self, "_lock_file"):
            self._lock_file.write_text(json.dumps({"ts": _now_ts(), "pid": os.getpid()}))

    def release_lock(self):
        if hasattr(self, "_lock_file"):
            try:
                self._lock_file.unlink()
            except FileNotFoundError:
                pass

    # ---- Operations ----
    def upsert_page(self, rec: PageRecord):
        cur = self.conn.cursor()
        cur.execute("BEGIN")
        try:
            cur.execute(
                """
                INSERT INTO notion_pages(
                  page_id,
                  page_title,
                  last_edited_time,
                  content_hash,
                  rfc_identifier,
                  status,
                  updated_at
                )
                VALUES(?,?,?,?,?,?,CURRENT_TIMESTAMP)
                ON CONFLICT(page_id) DO UPDATE SET
                  page_title=excluded.page_title,
                  last_edited_time=excluded.last_edited_time,
                  content_hash=excluded.content_hash,
                  rfc_identifier=excluded.rfc_identifier,
                  status=excluded.status,
                  updated_at=CURRENT_TIMESTAMP
                """,
                (rec.page_id, rec.page_title, rec.last_edited_time, rec.content_hash, rec.rfc_identifier, rec.status),
            )
            cur.execute("COMMIT")
        except Exception:
            cur.execute("ROLLBACK")
            raise

    def latest_issue_for_identifier(self, ident: str) -> Optional[Dict[str, Any]]:
        cur = self.conn.cursor()
        row = cur.execute(
            """
            SELECT gi.* FROM github_issues gi
            JOIN notion_pages np ON gi.notion_page_id = np.page_id
            WHERE UPPER(np.rfc_identifier)=UPPER(?)
            ORDER BY gi.created_at DESC LIMIT 1
            """,
            (ident,),
        ).fetchone()
        if not row:
            return None
        cols = [c[0] for c in cur.description]
        return dict(zip(cols, row))

    def record_issue(self, issue_number: int, issue_title: str, page_id: str, content_hash: str):
        cur = self.conn.cursor()
        cur.execute("BEGIN")
        try:
            cur.execute(
                """
                INSERT OR REPLACE INTO github_issues(
                  issue_number,
                  issue_title,
                  issue_state,
                  notion_page_id,
                  content_hash,
                  updated_at
                )
                VALUES(?,?,'open',?,?,CURRENT_TIMESTAMP)
                """,
                (issue_number, issue_title, page_id, content_hash),
            )
            cur.execute("COMMIT")
        except Exception:
            cur.execute("ROLLBACK")
            raise

    def close(self):
        self.conn.close()
        # promote temp DB atomically
        self.acquire_lock()
        try:
            shutil.move(self.tmp_path, self.original_path)
        finally:
            self.release_lock()
            try:
                shutil.rmtree(self.tmp_dir)
            except Exception:
                pass


@contextlib.contextmanager
def open_db(path: str):
    db = RfcDb(path)
    try:
        yield db
    finally:
        db.close()


def emit_summary(summary_path: str, **metrics: Any):
    existing: Dict[str, Any] = {}
    p = Path(summary_path)
    if p.exists():
        try:
            existing = json.loads(p.read_text())
        except Exception:
            existing = {}
    existing.update(metrics)
    p.write_text(json.dumps(existing, indent=2))


__all__ = [
    "open_db",
    "RfcDb",
    "PageRecord",
    "normalize_content",
    "stable_hash",
    "emit_summary",
]
