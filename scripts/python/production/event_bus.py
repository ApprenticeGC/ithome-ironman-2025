#!/usr/bin/env python3
"""Repository dispatch event emitter helpers (Flow-RFC-010)."""
from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.request
import uuid
from datetime import datetime, timezone
from typing import Any, Dict, Optional

API_URL_TEMPLATE = "https://api.github.com/repos/{repo}/dispatches"


class EventBusError(RuntimeError):
    pass


def _default_repo() -> str:
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        raise EventBusError("Repository must be provided via --repo or REPO/GITHUB_REPOSITORY env vars")
    return repo


def _token() -> str:
    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    if not token:
        raise EventBusError("GH_TOKEN or GITHUB_TOKEN must be set to emit events")
    return token


def emit_event(
    event_type: str,
    payload: Dict[str, Any],
    *,
    repo: Optional[str] = None,
    token: Optional[str] = None,
    event_id: Optional[str] = None,
    source: Optional[str] = None,
) -> Dict[str, Any]:
    """Emit a repository_dispatch event.

    Returns the payload that was sent (including metadata) for logging/testing.
    """
    repo = repo or _default_repo()
    token = token or _token()
    event_id = event_id or str(uuid.uuid4())
    source = source or os.environ.get("EVENT_SOURCE") or "automation"

    envelope = {
        "event_id": event_id,
        "source": source,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "payload": payload,
    }

    data = json.dumps({"event_type": event_type, "client_payload": envelope}).encode("utf-8")
    req = urllib.request.Request(
        API_URL_TEMPLATE.format(repo=repo),
        data=data,
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
            "Accept": "application/vnd.github+json",
            "User-Agent": "rfc-event-bus/1.0",
        },
    )
    try:
        with urllib.request.urlopen(req) as resp:
            # 204 expected, but we don't rely on status code here
            resp.read()
    except urllib.error.HTTPError as exc:  # type: ignore[attr-defined]
        body = exc.read().decode("utf-8", errors="replace") if hasattr(exc, "read") else ""
        raise EventBusError(f"GitHub API error {exc.code}: {body}") from exc
    except urllib.error.URLError as exc:  # type: ignore[attr-defined]
        raise EventBusError(f"Network error emitting event: {exc}") from exc

    return envelope


def _parse_args(argv: Optional[list[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Emit repository_dispatch events")
    parser.add_argument("event_type", help="Event type (used as repository_dispatch action)")
    parser.add_argument("payload", help="JSON payload to include under client_payload.payload")
    parser.add_argument("--repo", help="Target repository (owner/name)")
    parser.add_argument("--source", help="Event source identifier")
    parser.add_argument("--event-id", help="Explicit event id (defaults to UUID)")
    return parser.parse_args(argv)


def main(argv: Optional[list[str]] = None) -> int:
    args = _parse_args(argv)
    try:
        payload_obj = json.loads(args.payload)
    except json.JSONDecodeError as exc:
        raise EventBusError(f"Invalid JSON payload: {exc}") from exc

    envelope = emit_event(
        event_type=args.event_type,
        payload=payload_obj,
        repo=args.repo,
        event_id=args.event_id,
        source=args.source,
    )
    print(json.dumps({"status": "ok", "envelope": envelope}, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except EventBusError as exc:
        print(json.dumps({"status": "error", "message": str(exc)}), file=sys.stderr)
        sys.exit(1)
