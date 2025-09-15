#!/usr/bin/env python3
"""
Generate a shields.io JSON badge for current-month GitHub Actions runner usage.

Approach:
- List completed workflow runs for this repo (newest first) via REST API.
- Stop paging once we reach runs created before the first day of the month (UTC).
- For each run in the target window, list its jobs and sum job durations
  (completed_at - started_at). This approximates billed minutes.
- Write shields.io JSON to .github/badges/runner-usage.json

Environment:
- GITHUB_TOKEN: required (provided automatically in Actions)
- GITHUB_REPOSITORY: required (owner/repo, provided by Actions)

Output file:
- .github/badges/runner-usage.json
"""

from __future__ import annotations

import json
import os
import sys
import math
from datetime import datetime, timezone
from urllib import request, parse, error


API_BASE = "https://api.github.com"


def iso_utc(s: str) -> datetime:
    # Timestamps like 2023-09-01T12:34:56Z
    return datetime.strptime(s, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)


def http_get(url: str, token: str) -> dict:
    req = request.Request(url)
    req.add_header("Accept", "application/vnd.github+json")
    req.add_header("Authorization", f"Bearer {token}")
    req.add_header("X-GitHub-Api-Version", "2022-11-28")
    try:
        with request.urlopen(req) as resp:
            data = resp.read()
            return json.loads(data.decode("utf-8"))
    except error.HTTPError as e:
        msg = e.read().decode("utf-8", errors="ignore")
        raise RuntimeError(f"HTTP {e.code} for {url}: {msg}")


def month_bounds_utc(now: datetime | None = None) -> tuple[datetime, datetime]:
    now = now or datetime.now(timezone.utc)
    start = now.replace(day=1, hour=0, minute=0, second=0, microsecond=0)
    # Compute first day of next month
    if start.month == 12:
        next_month = start.replace(year=start.year + 1, month=1)
    else:
        next_month = start.replace(month=start.month + 1)
    return start, next_month


def list_runs_in_month(owner: str, repo: str, token: str, month_start: datetime, month_end: datetime) -> list[dict]:
    runs = []
    page = 1
    per_page = 100
    while True:
        url = f"{API_BASE}/repos/{owner}/{repo}/actions/runs?status=completed&per_page={per_page}&page={page}"
        payload = http_get(url, token)
        page_runs = payload.get("workflow_runs", [])
        if not page_runs:
            break

        for r in page_runs:
            created = iso_utc(r["created_at"]) if r.get("created_at") else None
            if created is None:
                continue
            if created < month_start:
                # We've paged past the month window; stop outer loops
                return runs
            if month_start <= created < month_end:
                runs.append(r)

        page += 1

    return runs


def sum_job_minutes_for_run(owner: str, repo: str, token: str, run_id: int) -> float:
    total_seconds = 0.0
    page = 1
    per_page = 100
    while True:
        url = f"{API_BASE}/repos/{owner}/{repo}/actions/runs/{run_id}/jobs?per_page={per_page}&page={page}"
        payload = http_get(url, token)
        jobs = payload.get("jobs", [])
        if not jobs:
            break

        for job in jobs:
            started_at = job.get("started_at")
            completed_at = job.get("completed_at")
            if not started_at or not completed_at:
                continue
            try:
                start_dt = iso_utc(started_at)
                end_dt = iso_utc(completed_at)
                if end_dt > start_dt:
                    total_seconds += (end_dt - start_dt).total_seconds()
            except Exception:
                # Skip problematic entries
                continue

        page += 1

    return total_seconds / 60.0


def pick_color(minutes: float) -> str:
    # Simple thresholds; tweak as desired
    if minutes <= 60:
        return "brightgreen"
    if minutes <= 300:
        return "blue"
    if minutes <= 1000:
        return "yellow"
    return "red"


def write_badge(path: str, minutes: float) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    rounded = int(round(minutes))
    badge = {
        "schemaVersion": 1,
        "label": "ci minutes (month)",
        "message": f"{rounded} min",
    "color": pick_color(minutes),
    "cacheSeconds": 3600,
    }
    with open(path, "w", encoding="utf-8") as f:
        json.dump(badge, f)


def main() -> int:
    token = os.getenv("GITHUB_TOKEN")
    repo_full = os.getenv("GITHUB_REPOSITORY")
    if not token or not repo_full or "/" not in repo_full:
        print("GITHUB_TOKEN and GITHUB_REPOSITORY are required.", file=sys.stderr)
        return 2

    owner, repo = repo_full.split("/", 1)

    month_start, month_end = month_bounds_utc()
    print(f"Calculating usage for {owner}/{repo} from {month_start.isoformat()} to {month_end.isoformat()} (UTC)")

    runs = list_runs_in_month(owner, repo, token, month_start, month_end)
    print(f"Found {len(runs)} completed runs in window")

    total_minutes = 0.0
    for r in runs:
        run_id = r.get("id")
        if run_id is None:
            continue
        minutes = sum_job_minutes_for_run(owner, repo, token, int(run_id))
        total_minutes += minutes

    print(f"Total minutes this month: {total_minutes:.2f}")

    badge_path = os.path.join(".github", "badges", "runner-usage.json")
    write_badge(badge_path, total_minutes)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
