#!/usr/bin/env python3
import json
import pathlib
import sys
import urllib.error
import urllib.request
from unittest import mock

import pytest

PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

import event_bus


class DummyResponse:
    def __init__(self, status: int = 204, payload: bytes | None = None):
        self.status = status
        self._payload = payload or b""

    def __enter__(self):
        return self

    def __exit__(self, *excinfo):
        return False

    def read(self):
        return self._payload


@mock.patch("urllib.request.urlopen")
@mock.patch("event_bus.uuid.uuid4", return_value="uuid-1234")
@mock.patch("event_bus.datetime")
def test_emit_event_envelope(datetime_mock, uuid_mock, urlopen_mock, monkeypatch):
    datetime_mock.now.return_value.isoformat.return_value = "2025-09-20T01:02:03+00:00"
    monkeypatch.setenv("GITHUB_TOKEN", "test-token")
    monkeypatch.setenv("GITHUB_REPOSITORY", "org/repo")
    urlopen_mock.return_value = DummyResponse()

    payload = {"issue": 123}
    envelope = event_bus.emit_event("rfc.issue.assigned", payload)

    request: urllib.request.Request = urlopen_mock.call_args.args[0]  # type: ignore[attr-defined]
    assert request.full_url.endswith("/repos/org/repo/dispatches")
    assert request.get_header("Authorization") == "Bearer test-token"

    sent = json.loads(request.data.decode("utf-8"))
    assert sent["event_type"] == "rfc.issue.assigned"
    assert sent["client_payload"]["event_id"] == "uuid-1234"
    assert sent["client_payload"]["payload"] == payload
    assert envelope == sent["client_payload"]


@mock.patch("urllib.request.urlopen", side_effect=urllib.error.HTTPError("url", 422, "Unprocessable", {}, None))
def test_emit_event_http_error(urlopen_mock, monkeypatch):
    monkeypatch.setenv("GITHUB_TOKEN", "tok")
    monkeypatch.setenv("GITHUB_REPOSITORY", "org/repo")
    with pytest.raises(event_bus.EventBusError):
        event_bus.emit_event("type", {})


@mock.patch("urllib.request.urlopen", side_effect=urllib.error.URLError("boom"))
def test_emit_event_network_error(urlopen_mock, monkeypatch):
    monkeypatch.setenv("GITHUB_TOKEN", "tok")
    monkeypatch.setenv("GITHUB_REPOSITORY", "org/repo")
    with pytest.raises(event_bus.EventBusError):
        event_bus.emit_event("type", {})
