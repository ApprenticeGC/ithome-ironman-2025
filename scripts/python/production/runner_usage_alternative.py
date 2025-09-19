#!/usr/bin/env python3
"""
Generate a shields.io JSON badge for GitHub Actions runner usage using workflow runs data.

This alternative approach uses workflow runs API instead of billing API.
It calculates approximate runner time by analyzing recent workflow runs.

Environment:
- GITHUB_TOKEN: required (provided automatically in Actions or from .env)
- GITHUB_REPOSITORY: required (owner/repo, provided by Actions or from .env)

Output file:
- .github/badges/runner-usage.json
"""

from __future__ import annotations

import json
import os
import sys
from datetime import datetime, timedelta
from pathlib import Path
from urllib import error, request

# Load environment variables from .env file if it exists
def load_env_file():
    """Load environment variables from .env file if it exists."""
    env_file = Path(__file__).parent.parent.parent.parent / ".env"
    if env_file.exists():
        with open(env_file, 'r') as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith('#') and '=' in line:
                    key, value = line.split('=', 1)
                    # Only set if not already in environment
                    if key not in os.environ:
                        os.environ[key] = value

# Load .env file if it exists
load_env_file()

API_BASE = "https://api.github.com"


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


def get_workflow_runs(owner: str, repo: str, token: str, days: int = 30) -> list:
    """Get workflow runs from the last N days."""
    # Calculate date threshold
    threshold = datetime.utcnow() - timedelta(days=days)
    threshold_str = threshold.isoformat() + "Z"

    # Get workflow runs
    url = f"{API_BASE}/repos/{owner}/{repo}/actions/runs"
    url += f"?created=>={threshold_str}&per_page=100"

    all_runs = []
    page = 1

    while page <= 10:  # Limit to 10 pages max
        paginated_url = f"{url}&page={page}"
        data = http_get(paginated_url, token)
        runs = data.get("workflow_runs", [])

        if not runs:
            break

        all_runs.extend(runs)
        page += 1

        # Stop if we have enough data or reached the end
        if len(runs) < 100:
            break

    return all_runs


def calculate_runner_time(runs: list) -> float:
    """Calculate total runner time in minutes from workflow runs."""
    total_seconds = 0

    for run in runs:
        if run.get("status") == "completed":
            created_at = run.get("created_at")
            updated_at = run.get("updated_at")

            if created_at and updated_at:
                try:
                    start = datetime.fromisoformat(created_at.rstrip("Z"))
                    end = datetime.fromisoformat(updated_at.rstrip("Z"))
                    duration = (end - start).total_seconds()

                    # Only count reasonable durations (< 4 hours)
                    if 0 < duration < 14400:
                        total_seconds += duration
                except (ValueError, TypeError):
                    continue

    return total_seconds / 60  # Convert to minutes


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
        "label": "runner time",
        "message": f"{rounded} min",
        "color": pick_color(minutes),
        "cacheSeconds": 3600,
    }
    with open(path, "w", encoding="utf-8") as f:
        json.dump(badge, f)


def main() -> int:
    token = os.getenv("GITHUB_TOKEN")
    repo_full = os.getenv("GITHUB_REPOSITORY")

    # Support both GITHUB_REPOSITORY format and separate GITHUB_OWNER/GITHUB_REPO
    if repo_full and "/" in repo_full:
        owner, repo = repo_full.split("/", 1)
    else:
        owner = os.getenv("GITHUB_OWNER")
        repo = os.getenv("GITHUB_REPO")
        repo_full = f"{owner}/{repo}" if owner and repo else None

    if not token or not owner or not repo:
        print("GITHUB_TOKEN and (GITHUB_REPOSITORY or GITHUB_OWNER+GITHUB_REPO) are required.", file=sys.stderr)
        return 2

    print(f"Getting workflow runs for {owner}/{repo}")

    try:
        runs = get_workflow_runs(owner, repo, token)
        total_minutes = calculate_runner_time(runs)

        print(f"Found {len(runs)} workflow runs in last 30 days")
        print(f"Estimated total runner time: {total_minutes:.1f} minutes")

    except RuntimeError as e:
        print(f"Error fetching workflow data: {e}", file=sys.stderr)
        return 1

    badge_path = os.getenv("BADGE_FILE_PATH", ".github/badges/runner-usage.json")
    write_badge(badge_path, total_minutes)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
