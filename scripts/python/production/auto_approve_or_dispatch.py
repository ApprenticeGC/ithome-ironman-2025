#!/usr/bin/env python3
import json
import os
import subprocess
import sys
from typing import List, Set

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")


def run(
    cmd: List[str],
    check: bool = True,
    capture: bool = True,
    extra_env: dict | None = None,
) -> subprocess.CompletedProcess:
    env = os.environ.copy()
    if extra_env:
        env.update(extra_env)
    return subprocess.run(
        cmd,
        check=check,
        text=True,
        capture_output=capture,
        env=env,
        encoding="utf-8",
        errors="replace",  # Handle Unicode decode errors gracefully
    )


def gh_json(cmd: List[str], extra_env: dict | None = None):
    res = run(cmd, extra_env=extra_env)
    if not res.stdout or not res.stdout.strip():
        raise ValueError(f"Empty response from command: {' '.join(cmd)}")
    return json.loads(res.stdout)


def branch_exists(repo: str, branch: str) -> bool:
    """Check if a branch exists in the repository."""
    try:
        run(
            ["gh", "api", f"repos/{repo}/branches/{branch}"],
            check=True,
            capture=True,
        )
        return True
    except subprocess.CalledProcessError:
        return False


def list_action_required_runs(repo: str):
    # Paginate through recent runs only (limit to 3 pages to avoid processing hundreds of old runs)
    runs: List[dict] = []
    page = 1
    max_pages = 3  # Limit to recent runs to avoid excessive processing
    while page <= max_pages:
        try:
            data = gh_json(["gh", "api", f"repos/{repo}/actions/runs?per_page=100&page={page}"])
        except subprocess.CalledProcessError as e:
            sys.stderr.write(f"list_action_required_runs error: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
            break
        page_runs = data.get("workflow_runs", [])
        if not page_runs:
            break
        for wr in page_runs:
            status = wr.get("status")
            conclusion = wr.get("conclusion")
            head = wr.get("head_branch", "")
            # Include both explicit action_required as well as "waiting" (often environment approval)
            if (status in ("action_required", "waiting") or conclusion == "action_required") and head.startswith(
                "copilot/"
            ):
                # Only process runs from existing branches
                if branch_exists(repo, head):
                    runs.append(wr)
                else:
                    print(f"Skipping action_required run for deleted branch: {head}")
        page += 1
    return runs


def list_open_copilot_pr_branches(repo: str) -> Set[str]:
    branches: Set[str] = set()
    page = 1
    while True:
        prs = gh_json(["gh", "api", f"repos/{repo}/pulls?state=open&per_page=100&page={page}"])
        if not isinstance(prs, list) or not prs:
            break
        for pr in prs:
            head_ref = (pr.get("head") or {}).get("ref", "")
            if head_ref.startswith("copilot/"):
                branches.add(head_ref)
        page += 1
    return branches


def approve_run(repo: str, run_id: int) -> bool:
    try:
        # Prefer PAT for approvals; fall back to default token
        token = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN", "")
        run(
            [
                "gh",
                "api",
                "-X",
                "POST",
                f"repos/{repo}/actions/runs/{run_id}/approve",
                "-H",
                "Accept: application/vnd.github+json",
            ],
            check=True,
            extra_env={"GH_TOKEN": token},
        )
        return True
    except subprocess.CalledProcessError as e:
        # Check if it's the expected "not from fork" error
        if "This run is not from a fork pull request" in str(e.stdout):
            # This is expected for non-fork runs, don't log as error
            return False
        sys.stderr.write(f"approve_run error for {run_id}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
        return False


def approve_pending_deployments(repo: str, run_id: int) -> bool:
    """Approve pending environment deployments for a workflow run (environment protection)."""
    try:
        token = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN", "")
        # List pending deployments to collect environment IDs
        resp = gh_json(
            ["gh", "api", f"repos/{repo}/actions/runs/{run_id}/pending_deployments"],
            extra_env={"GH_TOKEN": token},
        )
        pds = resp if isinstance(resp, list) else resp.get("pending_deployments", [])
        env_ids = []
        for pd in pds or []:
            env_obj = pd.get("environment") or {}
            if env_obj.get("id"):
                env_ids.append(str(env_obj["id"]))
        if not env_ids:
            return False
        # Approve all pending environments
        cmd = [
            "gh",
            "api",
            "-X",
            "POST",
            f"repos/{repo}/actions/runs/{run_id}/pending_deployments",
            "-H",
            "Accept: application/vnd.github+json",
            "-f",
            "state=approved",
            "-f",
            "comment=Auto-approved by Copilot bot",
        ]
        for eid in env_ids:
            cmd.extend(["-f", f"environment_ids[]={eid}"])
        run(cmd, check=True, extra_env={"GH_TOKEN": token})
        return True
    except subprocess.CalledProcessError as e:
        sys.stderr.write(
            f"approve_pending_deployments error for {run_id}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n"
        )
        return False


def rerun(repo: str, run_id: int) -> bool:
    try:
        # Attempt to rerun the workflow run after approval
        run(["gh", "run", "rerun", str(run_id)], check=True)
        return True
    except subprocess.CalledProcessError:
        return False


def dispatch_ci(repo: str, branch: str) -> bool:
    # Check if branch exists first
    if not branch_exists(repo, branch):
        print(f"Branch {branch} does not exist, skipping CI dispatch")
        return False

    # Try by workflow name, fall back to id
    # Use PAT if available to bypass approval gate
    env_pat = {}
    pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
    if pat:
        env_pat["GH_TOKEN"] = pat
    try:
        run(
            ["gh", "workflow", "run", "ci", "--ref", branch],
            check=True,
            extra_env=env_pat,
        )
        return True
    except subprocess.CalledProcessError as e1:
        sys.stderr.write(f"dispatch_ci name error for {branch}: {e1}\nSTDOUT: {e1.stdout}\nSTDERR: {e1.stderr}\n")
        try:
            wf_list = gh_json(["gh", "workflow", "list", "--json", "name,id"], extra_env=env_pat)
            ci_id = next((str(wf["id"]) for wf in wf_list if wf.get("name") == "ci"), None)
            if ci_id:
                run(
                    ["gh", "workflow", "run", ci_id, "--ref", branch],
                    check=True,
                    extra_env=env_pat,
                )
                return True
        except subprocess.CalledProcessError as e2:
            sys.stderr.write(f"dispatch_ci id error for {branch}: {e2}\nSTDOUT: {e2.stdout}\nSTDERR: {e2.stderr}\n")
    # Fallback: use ci-dispatch workflow with explicit input
    try:
        run(
            ["gh", "workflow", "run", "ci-dispatch", "-f", f"target_ref={branch}"],
            check=True,
            extra_env=env_pat,
        )
        return True
    except subprocess.CalledProcessError as e3:
        sys.stderr.write(f"dispatch_ci fallback error for {branch}: {e3}\nSTDOUT: {e3.stdout}\nSTDERR: {e3.stderr}\n")
    return False


def main():
    if not REPO:
        print("REPO not set", file=sys.stderr)
        sys.exit(1)

    print("Checking for pending workflow runs from Copilot...")
    runs = list_action_required_runs(REPO)
    if not runs:
        print("No pending Copilot workflow runs found")
        # As a fallback, check open PRs from copilot/* and try to dispatch CI for them
        pr_branches = list_open_copilot_pr_branches(REPO)
        if pr_branches:
            print(f"Found open Copilot PR branches: {', '.join(sorted(pr_branches))}")
            dispatched_any = False
            for br in sorted(pr_branches):
                print(f"Dispatching CI for {br} via workflow_dispatch fallback (no pending runs)...")
                if dispatch_ci(REPO, br):
                    print(f"Dispatched CI for {br}")
                    dispatched_any = True
                else:
                    print(f"Failed to dispatch CI for {br}")
            if not dispatched_any:
                # Surface failure for visibility in CI logs
                sys.exit(1)
        # If no PR branches either, nothing to do
        return

    branches: Set[str] = set()
    any_approved_or_reran = False
    print("Found pending workflow runs:", ",".join(str(r.get("id")) for r in runs))
    for wr in runs:
        run_id = wr.get("id")
        head = wr.get("head_branch", "")
        if head:
            branches.add(head)
        print(f"Approving workflow run {run_id} (branch={head})")
        ok = approve_run(REPO, run_id)
        if not ok:
            # If not a fork PR scenario, try approving pending environment deployments
            ok = approve_pending_deployments(REPO, run_id)
        if ok:
            print(f"Approved {run_id}")
            if rerun(REPO, run_id):
                print(f"Triggered rerun for {run_id}")
                any_approved_or_reran = True
            else:
                print(f"Rerun not needed or not possible for {run_id}")
        else:
            print(f"Approve endpoint failed for {run_id}")

    # Fallback: dispatch CI for affected copilot/* branches
    dispatched_any = False
    for br in sorted(b for b in branches if b.startswith("copilot/")):
        print(f"Dispatching CI for {br} via workflow_dispatch bypass...")
        if dispatch_ci(REPO, br):
            print(f"Dispatched CI for {br}")
            dispatched_any = True
        else:
            print(f"Failed to dispatch CI for {br}")

    print("Completed approval/dispatch process")
    # If we had pending runs but couldn't approve/rerun or dispatch any, fail for visibility
    if runs and not (any_approved_or_reran or dispatched_any):
        sys.exit(1)


if __name__ == "__main__":
    main()
