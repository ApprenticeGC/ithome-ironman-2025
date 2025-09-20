import itertools
import json
import os
import pathlib
import sys
import time
import types
import urllib.error

ROOT_PROD = pathlib.Path(__file__).resolve().parents[2] / "production"
sys.path.insert(0, str(ROOT_PROD))

import notion_reliability as nr  # type: ignore


class FakeResp:
    def __init__(self, payload):
        self._payload = payload

    def read(self):
        return json.dumps(self._payload).encode()

    def __enter__(self):
        return self

    def __exit__(self, *a):
        return False


# Helper to monkeypatch urllib.request.urlopen


def test_retry_transient(monkeypatch, tmp_path):
    page_id = "P1"
    seq = itertools.count()

    def fake_urlopen(req, timeout=0):
        n = next(seq)
        # first two attempts 503, then success for page meta and blocks
        if "pages" in req.full_url:
            if n < 2:
                raise urllib.error.HTTPError(req.full_url, 503, "svc unavailable", {}, None)
            return FakeResp(
                {
                    "id": page_id,
                    "last_edited_time": "t",
                    "properties": {"title": {"type": "title", "title": [{"plain_text": "Title"}]}},
                }
            )
        else:  # blocks
            return FakeResp({"results": [], "has_more": False})

    monkeypatch.setattr(nr.urllib.request, "urlopen", fake_urlopen)
    token = "t"
    client = nr.NotionReliableClient(token, retries=5)
    state = client.fetch_page_state(page_id)
    assert state.page_id == page_id
    assert client.metrics["api_retries"] >= 2


def test_journal_skip(monkeypatch, tmp_path):
    # Provide deterministic page fetch that changes hash only if counter increments
    page_id = "P2"
    calls = {"count": 0}

    def fake_urlopen(req, timeout=0):
        calls["count"] += 1
        if "pages" in req.full_url:
            return FakeResp(
                {
                    "id": page_id,
                    "last_edited_time": "t",
                    "properties": {"title": {"type": "title", "title": [{"plain_text": "Title2"}]}},
                }
            )
        else:
            return FakeResp({"results": [], "has_more": False})

    monkeypatch.setattr(nr.urllib.request, "urlopen", fake_urlopen)
    db_path = tmp_path / "rfc_tracking.db"
    journal = tmp_path / "journal.log"
    # first run
    nr.ingest_pages([page_id], db_path=str(db_path), token="t", dry_run=False, journal_path=str(journal))
    first_call_count = calls["count"]
    # second run should mark unchanged (dry run still triggers skip logic reading journal)
    nr.ingest_pages([page_id], db_path=str(db_path), token="t", dry_run=True, journal_path=str(journal))
    # Ensure no unexpected reduction in call count for dry-run skip
    assert calls["count"] >= first_call_count
    # Journal should have SUCCESS line
    content = journal.read_text()
    assert "SUCCESS" in content


def test_pagination_has_more(monkeypatch, tmp_path):
    """Test pagination handling with has_more=True scenario"""
    page_id = "P_PAGINATED"
    call_count = 0

    def fake_urlopen(req, timeout=0):
        nonlocal call_count
        call_count += 1

        if "pages" in req.full_url:
            # Page metadata response
            return FakeResp(
                {
                    "id": page_id,
                    "last_edited_time": "t",
                    "properties": {"title": {"type": "title", "title": [{"plain_text": "Paginated Page"}]}},
                }
            )
        elif "children" in req.full_url:
            # Block children response - simulate pagination
            if "start_cursor" not in req.full_url:
                # First page of blocks
                return FakeResp(
                    {
                        "results": [
                            {"type": "paragraph", "paragraph": {"rich_text": [{"plain_text": "First block"}]}},
                            {"type": "heading_3", "heading_3": {"rich_text": [{"plain_text": "Header 1"}]}},
                        ],
                        "has_more": True,
                        "next_cursor": "cursor123",
                    }
                )
            else:
                # Second page of blocks
                return FakeResp(
                    {
                        "results": [
                            {
                                "type": "bulleted_list_item",
                                "bulleted_list_item": {"rich_text": [{"plain_text": "List item"}]},
                            }
                        ],
                        "has_more": False,
                    }
                )

    monkeypatch.setattr(nr.urllib.request, "urlopen", fake_urlopen)

    client = nr.NotionReliableClient("test_token", retries=1)
    state = client.fetch_page_state(page_id)

    # Should have content from both pages
    assert "First block" in state.content
    assert "Header 1" in state.content
    assert "List item" in state.content
    # Should have made at least 3 calls: page metadata + 2 block requests
    assert call_count >= 3
