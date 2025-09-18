#!/usr/bin/env python3
"""
Generate a shields.io JSON badge for GitHub Actions runner usage using official billing API.

Approach:
- Use GitHub's official billing API to get accurate Actions usage data
- Create a badge showing total minutes used across all runner types
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
from urllib import error, request

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


def get_actions_billing(owner: str, token: str) -> dict:
    """Get GitHub Actions billing information for a user or organization."""
    # Try organization endpoint first, fallback to user endpoint
    try:
        url = f"{API_BASE}/orgs/{owner}/settings/billing/actions"
        return http_get(url, token)
    except RuntimeError as e:
        if "404" in str(e):
            # Fallback to user endpoint
            url = f"{API_BASE}/users/{owner}/settings/billing/actions"
            return http_get(url, token)
        raise


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
    if not token or not repo_full or "/" not in repo_full:
        print("GITHUB_TOKEN and GITHUB_REPOSITORY are required.", file=sys.stderr)
        return 2

    owner, repo = repo_full.split("/", 1)

    print(f"Getting Actions billing for {owner}")

    try:
        billing_data = get_actions_billing(owner, token)
        total_minutes = billing_data.get("total_minutes_used", 0)

        print(f"Total minutes used: {total_minutes}")
        print(f"Included minutes: {billing_data.get('included_minutes', 0)}")
        print(f"Paid minutes: {billing_data.get('total_paid_minutes_used', 0)}")

        # Show breakdown by runner type
        breakdown = billing_data.get("minutes_used_breakdown", {})
        for runner_type, minutes in breakdown.items():
            print(f"  {runner_type}: {minutes} minutes")

    except RuntimeError as e:
        print(f"Error fetching billing data: {e}", file=sys.stderr)
        return 1

    badge_path = os.path.join(".github", "badges", "runner-usage.json")
    write_badge(badge_path, total_minutes)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
