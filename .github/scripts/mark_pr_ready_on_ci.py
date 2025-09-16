#!/usr/bin/env python3
import json
import os
import subprocess
import sys
from typing import Optional

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")
EVENT_JSON = os.environ.get("GITHUB_EVENT", "")

def run(cmd, check=True):
    return subprocess.run(cmd, check=check, text=True, capture_output=True)

def gh_json(cmd):
    res = run(cmd)
    return json.loads(res.stdout)

def find_pr_number_by_branch(repo: str, branch: str) -> Optional[int]:
    try:
        out = run(["gh", "pr", "list", "--repo", repo, "--state", "open", "--json", "number,headRefName"]).stdout
        items = json.loads(out)
        for it in items:
            if it.get("headRefName") == branch:
                return it.get("number")
    except subprocess.CalledProcessError:
        return None
    return None

def mark_ready(repo: str, pr_number: int):
    try:
        pr_id = run(["gh", "pr", "view", str(pr_number), "--repo", repo, "--json", "id", "-q", ".id"]).stdout.strip()
        run(["gh", "api", "graphql", "-f", "query=mutation($id: ID!) { markPullRequestReadyForReview(input: {pullRequestId: $id}) { clientMutationId } }", "-F", f"id={pr_id}"])
        return True
    except subprocess.CalledProcessError:
        return False

def main():
    if not REPO:
        print("REPO not set", file=sys.stderr)
        sys.exit(1)

    if not EVENT_JSON:
        EVENT_JSON = os.environ.get("GITHUB_EVENT_PATH") and open(os.environ["GITHUB_EVENT_PATH"], "r", encoding="utf-8").read() or "{}"
    evt = json.loads(EVENT_JSON)
    wr = evt.get("workflow_run", {})

    # Try PR from payload first
    pr_number = None
    prs = wr.get("pull_requests") or []
    if prs:
        pr_number = prs[0].get("number")

    if pr_number is None:
        branch = wr.get("head_branch")
        if not branch:
            print("No branch to derive PR", file=sys.stderr)
            sys.exit(0)
        pr_number = find_pr_number_by_branch(REPO, branch)

    if pr_number is None:
        print("No associated PR found")
        sys.exit(0)

    pr = gh_json(["gh", "pr", "view", str(pr_number), "--repo", REPO, "--json", "draft,title,author,headRepositoryOwner,baseRepository"])
    if not pr.get("draft", True):
        print("Not draft; nothing to do")
        sys.exit(0)

    author = pr.get("author", {}).get("login", "")
    title = pr.get("title", "")
    base = (pr.get("baseRepository") or {}).get("nameWithOwner", "")

    if author not in {"Copilot", "app/copilot-swe-agent", "github-actions[bot]", "github-actions"}:
        print("Non-Copilot author; skipping")
        sys.exit(0)
    if "RFC-" not in title:
        print("No RFC tag in title; skipping")
        sys.exit(0)
    if base != REPO:
        print("Different base repo; skipping")
        sys.exit(0)

    if mark_ready(REPO, pr_number):
        print(f"PR #{pr_number} marked ready for review")
    else:
        print(f"Failed to mark PR #{pr_number} ready", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
