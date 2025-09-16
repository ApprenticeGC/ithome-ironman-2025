#!/usr/bin/env python3
import json
import os
import subprocess
import sys
from typing import Optional

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")
EVENT_JSON = os.environ.get("GITHUB_EVENT", "")


def run(cmd, check=True, extra_env: Optional[dict] = None):
    env = os.environ.copy()
    if extra_env:
        env.update(extra_env)
    return subprocess.run(cmd, check=check, text=True, capture_output=True, env=env)


def gh_json(cmd, extra_env: Optional[dict] = None):
    res = run(cmd, extra_env=extra_env)
    return json.loads(res.stdout)


def find_pr_number_by_branch(repo: str, branch: str) -> Optional[int]:
    out = run(["gh", "pr", "list", "--repo", repo, "--state", "open", "--json", "number,headRefName"], extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")}).stdout
    items = json.loads(out)
    for it in items:
        if it.get("headRefName") == branch:
            return it.get("number")
    return None


def try_enable_automerge(repo: str, pr_number: int) -> bool:
    try:
        # Resolve PR node ID
    pr_id = run(["gh", "pr", "view", str(pr_number), "--repo", repo, "--json", "id", "-q", ".id"], extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")}).stdout.strip()
        if not pr_id:
            return False
        # Use GraphQL to enable auto-merge with SQUASH method
        query = (
            "mutation($id: ID!, $method: PullRequestMergeMethod!) { "
            "enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: $method}) { clientMutationId } }"
        )
    run(["gh", "api", "graphql", "-f", f"query={query}", "-F", f"id={pr_id}", "-F", "method=SQUASH"], check=True, extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")})
        return True
    except subprocess.CalledProcessError as e:
        # Bubble stderr for diagnostics and return False
        sys.stderr.write(e.stderr or "")
        return False


def add_comment(repo: str, pr_number: int, body: str) -> None:
    run(["gh", "pr", "comment", str(pr_number), "--repo", repo, "--body", body], check=False, extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")})


def main():
    if not REPO:
        print("REPO not set", file=sys.stderr)
        sys.exit(1)

    if not EVENT_JSON:
        EVENT_JSON = os.environ.get("GITHUB_EVENT_PATH") and open(os.environ["GITHUB_EVENT_PATH"], "r", encoding="utf-8").read() or "{}"
    evt = json.loads(EVENT_JSON)

    wr = evt.get("workflow_run", {})
    conclusion = wr.get("conclusion")
    if conclusion != "success":
        print("Not a successful workflow_run; exiting")
        return

    pr_number = None
    prs = wr.get("pull_requests") or []
    if prs:
        pr_number = prs[0].get("number")

    if pr_number is None:
        branch = wr.get("head_branch")
        if branch:
            pr_number = find_pr_number_by_branch(REPO, branch)

    if pr_number is None:
        print("No associated PR found")
        return

    pr = gh_json(["gh", "pr", "view", str(pr_number), "--repo", REPO, "--json", "number,title,author,draft,mergeStateStatus,autoMergeRequest,baseRepository"], extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")})
    title = pr.get("title", "")
    author = pr.get("author", {}).get("login", "")
    draft = pr.get("draft", True)
    base = (pr.get("baseRepository") or {}).get("nameWithOwner", "")

    if author not in {"Copilot", "app/copilot-swe-agent", "github-actions[bot]", "github-actions"}:
        print("Non-Copilot author; skipping")
        return
    if "RFC-" not in title:
        print("No RFC tag in title; skipping")
        return
    if base != REPO:
        print("Different base repo; skipping")
        return
    if draft:
        print("PR still draft; skipping auto-merge enable")
        return

    # Skip if auto-merge already requested
    if pr.get("autoMergeRequest"):
        print("Auto-merge already enabled")
        return

    if try_enable_automerge(REPO, pr_number):
        print(f"Enabled auto-merge for PR #{pr_number}")
        return

    # If auto-merge could not be enabled, add a diagnostic comment
    body = (
        "Auto-merge could not be enabled automatically. Possible reasons:\n"
        "- Repository setting 'Allow auto-merge' is disabled\n"
        "- Required checks are not satisfied or misconfigured\n"
        "- Actions token lacks permissions to enable auto-merge\n"
        "- Branch protection prevents auto-merge\n"
        "\nAction taken: None. Please adjust settings or enable auto-merge manually."
    )
    add_comment(REPO, pr_number, body)
    print("Posted diagnostic comment")


if __name__ == "__main__":
    main()
