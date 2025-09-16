#!/usr/bin/env python3
"""
Periodic PR flow monitor: ensures CI ran, PR undrafted, and auto-merge enabled.
- Finds open Copilot PRs with RFC titles
- If no successful CI run exists, attempts to dispatch CI (trusted token)
- If PR is draft and CI previously succeeded, marks ready
- If PR is ready, enables auto-merge via GraphQL (SQUASH)
Exits 0 always to avoid flapping; logs actions for observability.
"""
import json
import os
import subprocess
import sys
from typing import List

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")


def run(cmd: List[str], check: bool = True):
    return subprocess.run(cmd, check=check, text=True, capture_output=True)


def gh_json(cmd: List[str]):
    res = run(cmd)
    return json.loads(res.stdout)


def list_open_copilot_prs(repo: str):
    prs = gh_json(["gh", "pr", "list", "--repo", repo, "--state", "open", "--json", "number,title,author,isDraft,headRefName,baseRepository,autoMergeRequest"])
    res = []
    for pr in prs:
        author = (pr.get("author") or {}).get("login", "")
        title = pr.get("title", "")
        base = (pr.get("baseRepository") or {}).get("nameWithOwner", "")
        if author in {"Copilot", "app/copilot-swe-agent"} and "RFC-" in title and base == repo:
            res.append(pr)
    return res


def has_success_ci(repo: str, branch: str) -> bool:
    runs = gh_json(["gh", "api", f"repos/{repo}/actions/runs", "--jq", ".workflow_runs[] | select(.head_branch == \"%s\" and .name == \"ci\") | {conclusion}" % branch])
    # When using --jq on gh api, stdout is lines of JSON; we safeguard with parse
    try:
        if isinstance(runs, list):
            return any((r.get("conclusion") == "success") for r in runs)
    except Exception:
        pass
    # Fallback: query via gh run list (could be less reliable in script context)
    try:
        out = run(["gh", "run", "list", "--repo", repo, "--workflow", "ci", "--json", "databaseId,headBranch,conclusion"]).stdout
        arr = json.loads(out)
        return any((r.get("headBranch") == branch and r.get("conclusion") == "success") for r in arr)
    except subprocess.CalledProcessError:
        return False


def dispatch_ci(repo: str, branch: str) -> bool:
    try:
        run(["gh", "workflow", "run", "ci", "--ref", branch], check=True)
        return True
    except subprocess.CalledProcessError:
        return False


def mark_ready(repo: str, pr_number: int) -> bool:
    try:
        pr_id = run(["gh", "pr", "view", str(pr_number), "--repo", repo, "--json", "id", "-q", ".id"]).stdout.strip()
        run(["gh", "api", "graphql", "-f", "query=mutation($id: ID!) { markPullRequestReadyForReview(input: {pullRequestId: $id}) { clientMutationId } }", "-F", f"id={pr_id}"], check=True)
        return True
    except subprocess.CalledProcessError:
        return False


def enable_automerge(repo: str, pr_number: int) -> bool:
    try:
        pr_id = run(["gh", "pr", "view", str(pr_number), "--repo", repo, "--json", "id", "-q", ".id"]).stdout.strip()
        query = (
            "mutation($id: ID!, $method: PullRequestMergeMethod!) { "
            "enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: $method}) { clientMutationId } }"
        )
        run(["gh", "api", "graphql", "-f", f"query={query}", "-F", f"id={pr_id}", "-F", "method=SQUASH"], check=True)
        return True
    except subprocess.CalledProcessError:
        return False


def main():
    if not REPO:
        print("REPO not set", file=sys.stderr)
        return

    prs = list_open_copilot_prs(REPO)
    for pr in prs:
        number = pr.get("number")
        branch = pr.get("headRefName")
        is_draft = pr.get("isDraft", True)
        print(f"Monitor PR #{number} {branch} draft={is_draft}")

        # Ensure CI ran successfully
        if not has_success_ci(REPO, branch):
            if dispatch_ci(REPO, branch):
                print(f"Dispatched CI for {branch}")
            else:
                print(f"Could not dispatch CI for {branch}")
            continue  # Wait for CI

        # Undraft and enable auto-merge
        if is_draft:
            if mark_ready(REPO, number):
                print(f"Marked PR #{number} ready")
            else:
                print(f"Failed to mark PR #{number} ready")
                continue
        if not pr.get("autoMergeRequest"):
            if enable_automerge(REPO, number):
                print(f"Enabled auto-merge for PR #{number}")
            else:
                print(f"Failed to enable auto-merge for PR #{number}")


if __name__ == "__main__":
    main()
