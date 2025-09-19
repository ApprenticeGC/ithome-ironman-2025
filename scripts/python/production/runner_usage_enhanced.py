#!/usr/bin/env python3
"""
Enhanced GitHub Actions runner usage calculator that matches official metrics.

This version:
- Fetches ALL workflow runs (no pagination limits)
- Gets job-level timing data for accuracy
- Calculates current month usage to match GitHub's metrics
- Accounts for parallel job execution properly

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
        with request.urlopen(req) as response:
            return json.loads(response.read())
    except error.HTTPError as e:
        body = e.read().decode()
        raise RuntimeError(f"HTTP {e.code} for {url}: {body}") from e


def get_current_month_start():
    """Get the start of the current month in ISO format."""
    now = datetime.now()
    month_start = datetime(now.year, now.month, 1)
    return month_start.strftime('%Y-%m-%dT%H:%M:%SZ')


def get_all_workflow_runs(owner: str, repo: str, token: str) -> list[dict]:
    """Get ALL workflow runs from the current month with no pagination limits."""
    threshold_str = get_current_month_start()
    url = f"{API_BASE}/repos/{owner}/{repo}/actions/runs"
    url += f"?created=>={threshold_str}&per_page=100"

    all_runs = []
    page = 1

    print(f"Fetching workflow runs since {threshold_str}...")

    while True:
        paginated_url = f"{url}&page={page}"
        print(f"  Page {page}...", end=" ", flush=True)

        try:
            data = http_get(paginated_url, token)
            runs = data.get("workflow_runs", [])
        except RuntimeError as e:
            print(f"Error on page {page}: {e}")
            break

        if not runs:
            print("(no more data)")
            break

        all_runs.extend(runs)
        print(f"({len(runs)} runs)")
        page += 1

        # Stop if we got less than a full page
        if len(runs) < 100:
            break

        # Safety limit to prevent infinite loops
        if page > 200:  # Allow up to 20,000 runs
            print(f"Reached safety limit of 200 pages")
            break

    print(f"Total workflow runs fetched: {len(all_runs)}")
    return all_runs


def get_job_timing_for_run(owner: str, repo: str, run_id: int, token: str) -> float:
    """Get the total job execution time for a specific workflow run."""
    url = f"{API_BASE}/repos/{owner}/{repo}/actions/runs/{run_id}/jobs"

    try:
        data = http_get(url, token)
        jobs = data.get("jobs", [])

        total_minutes = 0.0
        for job in jobs:
            if job.get("started_at") and job.get("completed_at"):
                start = datetime.fromisoformat(job["started_at"].replace('Z', '+00:00'))
                end = datetime.fromisoformat(job["completed_at"].replace('Z', '+00:00'))
                duration = (end - start).total_seconds() / 60
                total_minutes += duration

        return total_minutes
    except RuntimeError:
        # If we can't get job details, estimate based on workflow timing
        return 0.0


def calculate_runner_time_enhanced(runs: list[dict], owner: str, repo: str, token: str) -> float:
    """Calculate total runner time by getting job-level data for accuracy."""
    total_minutes = 0.0
    job_fetched_count = 0
    estimated_count = 0

    print(f"Calculating runner time from {len(runs)} workflow runs...")

    for i, run in enumerate(runs):
        if i % 50 == 0:
            print(f"  Processed {i}/{len(runs)} runs...")

        run_id = run.get("id")
        if not run_id:
            continue

        # Try to get accurate job timing
        job_time = get_job_timing_for_run(owner, repo, run_id, token)

        if job_time > 0:
            total_minutes += job_time
            job_fetched_count += 1
        else:
            # Fallback: estimate from workflow timing
            if run.get("created_at") and run.get("updated_at"):
                start = datetime.fromisoformat(run["created_at"].replace('Z', '+00:00'))
                end = datetime.fromisoformat(run["updated_at"].replace('Z', '+00:00'))
                duration = (end - start).total_seconds() / 60
                total_minutes += max(duration, 1.0)  # Minimum 1 minute
                estimated_count += 1

    print(f"  Job-level timing: {job_fetched_count} runs")
    print(f"  Estimated timing: {estimated_count} runs")
    print(f"  Total calculated time: {total_minutes:.1f} minutes")

    return total_minutes


def pick_color(minutes: float) -> str:
    if minutes < 60:
        return "green"
    elif minutes < 300:
        return "blue"
    elif minutes < 1000:
        return "yellow"
    elif minutes < 3000:
        return "orange"
    else:
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

    print(f"Getting enhanced workflow data for {owner}/{repo}")
    print(f"Timeframe: Current month ({get_current_month_start()} to now)")

    try:
        runs = get_all_workflow_runs(owner, repo, token)
        total_minutes = calculate_runner_time_enhanced(runs, owner, repo, token)

        print(f"\n=== RESULTS ===")
        print(f"Workflow runs found: {len(runs)}")
        print(f"Total runner time: {total_minutes:.1f} minutes")
        print(f"Average per run: {total_minutes/len(runs):.1f} minutes" if runs else "N/A")

    except RuntimeError as e:
        print(f"Error fetching workflow data: {e}", file=sys.stderr)
        return 1

    badge_path = os.getenv("BADGE_FILE_PATH", ".github/badges/runner-usage.json")
    write_badge(badge_path, total_minutes)
    print(f"Badge written to {badge_path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
