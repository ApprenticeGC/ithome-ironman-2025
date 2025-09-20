import json
import os
import pathlib
import sys
import threading
from pathlib import Path

ROOT = pathlib.Path(__file__).resolve().parents[2] / "production"
sys.path.insert(0, str(ROOT))
import rfc_db_v2 as dbv2  # type: ignore


def test_normalize_and_hash_idempotent(tmp_path):
    raw = "Line 1\r\n\n\nLine 2  \n"
    n1 = dbv2.normalize_content(raw)
    n2 = dbv2.normalize_content(n1)
    assert n1 == n2
    h1 = dbv2.stable_hash(raw)
    h2 = dbv2.stable_hash(raw)
    assert h1 == h2


def test_db_migration_and_upsert(tmp_path):
    db_file = tmp_path / "rfc_tracking.db"
    with dbv2.open_db(str(db_file)) as db:
        db.upsert_page(
            dbv2.PageRecord(
                page_id="p1", page_title="T", last_edited_time="ts", content_hash="h", rfc_identifier="RFC-001-01"
            )
        )
    # reopen ensure record there
    with dbv2.open_db(str(db_file)) as db:
        rec = db.latest_issue_for_identifier("RFC-001-01")
        assert rec is None  # no issue yet


def test_record_issue(tmp_path):
    db_file = tmp_path / "rfc_tracking.db"
    with dbv2.open_db(str(db_file)) as db:
        db.upsert_page(
            dbv2.PageRecord(
                page_id="p2", page_title="T2", last_edited_time="ts", content_hash="h2", rfc_identifier="RFC-001-02"
            )
        )
        db.record_issue(10, "RFC-001-02: Title", "p2", "h2")
    with dbv2.open_db(str(db_file)) as db:
        issue = db.latest_issue_for_identifier("RFC-001-02")
        assert issue is not None
        assert issue["issue_number"] == 10


def test_lock_contention(tmp_path):
    db_file = tmp_path / "rfc_tracking.db"
    # create initial
    with dbv2.open_db(str(db_file)) as db:
        db.upsert_page(
            dbv2.PageRecord(
                page_id="p3", page_title="T3", last_edited_time="ts", content_hash="h3", rfc_identifier="RFC-001-03"
            )
        )
    # simulate two openers
    results = []

    def worker():
        with dbv2.open_db(str(db_file)) as db:
            db.upsert_page(
                dbv2.PageRecord(
                    page_id="p3",
                    page_title="T3b",
                    last_edited_time="ts",
                    content_hash="h3b",
                    rfc_identifier="RFC-001-03",
                )
            )
            results.append(True)

    t1 = threading.Thread(target=worker)
    t2 = threading.Thread(target=worker)
    t1.start()
    t2.start()
    t1.join()
    t2.join()
    assert len(results) == 2


def test_reopen_idempotency(tmp_path):
    db_file = tmp_path / "rfc_tracking.db"
    raw_content = "Line A\n\nLine B\n"
    content_hash = dbv2.stable_hash(raw_content, extra={"rfc": "RFC-XYZ-01"})
    # first open
    with dbv2.open_db(str(db_file)) as db:
        db.upsert_page(
            dbv2.PageRecord(
                page_id="r1",
                page_title="Reopen Test",
                last_edited_time="t1",
                content_hash=content_hash,
                rfc_identifier="RFC-XYZ-01",
            )
        )
    # second open - ensure schema_version exists and hash stable
    with dbv2.open_db(str(db_file)) as db:
        cur = db.conn.cursor()
        v = cur.execute("SELECT MAX(version) FROM schema_version").fetchone()[0]
        assert v >= 1
        row = cur.execute("SELECT content_hash FROM notion_pages WHERE page_id=?", ("r1",)).fetchone()
        assert row and row[0] == content_hash
    # third open - no changes, still same
    with dbv2.open_db(str(db_file)) as db:
        cur = db.conn.cursor()
        row2 = cur.execute("SELECT content_hash FROM notion_pages WHERE page_id=?", ("r1",)).fetchone()
        assert row2 and row2[0] == content_hash
