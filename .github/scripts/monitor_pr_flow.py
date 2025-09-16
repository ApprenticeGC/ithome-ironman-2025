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
from typing import List, Optional

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")


def run(cmd: List[str], check: bool = True, extra_env: Optional[dict] = None):
    env = os.environ.copy()
    if extra_env:
        env.update(extra_env)
    return subprocess.run(cmd, check=check, text=True, capture_output=True, env=env)


def gh_json(cmd: List[str], extra_env: Optional[dict] = None):
    res = run(cmd, extra_env=extra_env)
    return json.loads(res.stdout)


def list_open_copilot_prs(repo: str):
    # Use REST API to avoid gh pr list flakiness; paginate for completeness
    page = 1
    found = []
    while True:
        try:
            # Correct pulls list endpoint with query params
            prs = gh_json([
                "gh", "api", f"repos/{repo}/pulls?state=open&per_page=100&page={page}"
            ])
        except subprocess.CalledProcessError as e:
            sys.stderr.write(f"list_open_copilot_prs error: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
            break
        if not isinstance(prs, list) or not prs:
            break
        for pr in prs:
            head = (pr.get("head") or {}).get("ref", "")
            base_repo = (pr.get("base") or {}).get("repo") or {}
            base_full = base_repo.get("full_name", "")
            title = pr.get("title", "")
            # Target Copilot-authored branches in this repo; do not hard-require RFC token
            if head.startswith("copilot/") and base_full == repo:
                found.append({
                    "number": pr.get("number"),
                    "headRefName": head,
                    "isDraft": pr.get("draft", True),
                    "autoMergeRequest": pr.get("auto_merge")
                })
        page += 1
    return found


def has_success_ci(repo: str, branch: str) -> bool:
    # Find workflow id for 'ci'
    try:
    wfs = gh_json(["gh", "workflow", "list", "--repo", repo, "--json", "name,id"])
        ci_id = next((str(wf["id"]) for wf in wfs if wf.get("name") == "ci"), None)
        if not ci_id:
            return False
    # The runs endpoint: use query params instead of -F to avoid method confusion
    data = gh_json(["gh", "api", f"repos/{repo}/actions/workflows/{ci_id}/runs?branch={branch}&per_page=20"])
        for run in data.get("workflow_runs", []) or []:
            if run.get("head_branch") == branch and run.get("conclusion") == "success":
                return True
        return False
    except subprocess.CalledProcessError as e:
        sys.stderr.write(f"has_success_ci error for {branch}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
        return False


def dispatch_ci(repo: str, branch: str) -> bool:
    # Prefer PAT if available
    env_pat = {}
    pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
    if pat:
        env_pat["GH_TOKEN"] = pat
    try:
        run(["gh", "workflow", "run", "ci", "--ref", branch], check=True, extra_env=env_pat)
        return True
    except subprocess.CalledProcessError as e1:
        sys.stderr.write(f"dispatch_ci name error for {branch}: {e1}\nSTDOUT: {e1.stdout}\nSTDERR: {e1.stderr}\n")
        try:
            wfs = gh_json(["gh", "workflow", "list", "--repo", repo, "--json", "name,id"], extra_env=env_pat)
            ci_id = next((str(wf["id"]) for wf in wfs if wf.get("name") == "ci"), None)
            if ci_id:
                run(["gh", "workflow", "run", ci_id, "--ref", branch], check=True, extra_env=env_pat)
                return True
        except subprocess.CalledProcessError as e2:
            sys.stderr.write(f"dispatch_ci id error for {branch}: {e2}\nSTDOUT: {e2.stdout}\nSTDERR: {e2.stderr}\n")
    return False


def mark_ready(repo: str, pr_number: int) -> bool:
    try:
        env_pat = {}
        pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
        if pat:
            env_pat["GH_TOKEN"] = pat
        pr_id = run(["gh", "pr", "view", str(pr_number), "--repo", repo, "--json", "id", "-q", ".id"], extra_env=env_pat).stdout.strip()
        run(["gh", "api", "graphql", "-f", "query=mutation($id: ID!) { markPullRequestReadyForReview(input: {pullRequestId: $id}) { clientMutationId } }", "-F", f"id={pr_id}"], check=True, extra_env=env_pat)
        return True
    except subprocess.CalledProcessError as e:
        sys.stderr.write(f"mark_ready error for PR #{pr_number}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
        return False


def enable_automerge(repo: str, pr_number: int) -> bool:
    try:
        env_pat = {}
        pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
        if pat:
            env_pat["GH_TOKEN"] = pat
        pr_id = run(["gh", "pr", "view", str(pr_number), "--repo", repo, "--json", "id", "-q", ".id"], extra_env=env_pat).stdout.strip()
        query = (
            "mutation($id: ID!, $method: PullRequestMergeMethod!) { "
            "enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: $method}) { clientMutationId } }"
        )
        run(["gh", "api", "graphql", "-f", f"query={query}", "-F", f"id={pr_id}", "-F", "method=SQUASH"], check=True, extra_env=env_pat)
        return True
    except subprocess.CalledProcessError as e:
        sys.stderr.write(f"enable_automerge error for PR #{pr_number}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
        return False


def main():
    if not REPO:
        print("REPO not set", file=sys.stderr)
        return
    try:
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
    except Exception as e:
        # Never fail this monitor; just log the issue
        sys.stderr.write(f"monitor_pr_flow unexpected error: {e}\n")


if __name__ == "__main__":
    main()
