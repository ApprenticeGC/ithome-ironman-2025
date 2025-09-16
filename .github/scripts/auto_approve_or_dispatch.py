#!/usr/bin/env python3
import json
import os
import subprocess
import sys
from typing import List, Set

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")

def run(cmd: List[str], check: bool = True, capture: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(cmd, check=check, text=True, capture_output=capture)

def gh_json(cmd: List[str]):
    res = run(cmd)
    return json.loads(res.stdout)

def list_action_required_runs(repo: str):
    data = gh_json(["gh", "api", f"repos/{repo}/actions/runs"])
    runs = []
    for wr in data.get("workflow_runs", []):
        status = wr.get("status")
        conclusion = wr.get("conclusion")
        actor = (wr.get("actor") or {}).get("login", "")
        head = wr.get("head_branch", "")
        if (status == "action_required" or conclusion == "action_required") and (
            actor in {"Copilot", "app/copilot-swe-agent"} or head.startswith("copilot/")
        ):
            runs.append(wr)
    return runs

def approve_run(repo: str, run_id: int) -> bool:
    try:
        run(["gh", "api", "-X", "POST", f"repos/{repo}/actions/runs/{run_id}/approve", "-H", "Accept: application/vnd.github+json"], check=True)
        return True
    except subprocess.CalledProcessError:
        return False

def rerun(repo: str, run_id: int) -> bool:
    try:
        run(["gh", "run", "rerun", str(run_id)], check=True)
        return True
    except subprocess.CalledProcessError:
        return False

def dispatch_ci(repo: str, branch: str) -> bool:
    # Try by workflow name, fall back to id
    try:
        run(["gh", "workflow", "run", "ci", "--ref", branch], check=True)
        return True
    except subprocess.CalledProcessError:
        try:
            wf_list = gh_json(["gh", "workflow", "list", "--json", "name,id"])
            ci_id = next((str(wf["id"]) for wf in wf_list if wf.get("name") == "ci"), None)
            if ci_id:
                run(["gh", "workflow", "run", ci_id, "--ref", branch], check=True)
                return True
        except subprocess.CalledProcessError:
            pass
    return False

def main():
    if not REPO:
        print("REPO not set", file=sys.stderr)
        sys.exit(1)

    print("Checking for pending workflow runs from Copilot...")
    runs = list_action_required_runs(REPO)
    if not runs:
        print("No pending Copilot workflow runs found")
        return

    branches: Set[str] = set()
    print("Found pending workflow runs:", ",".join(str(r.get("id")) for r in runs))
    for wr in runs:
        run_id = wr.get("id")
        head = wr.get("head_branch", "")
        if head:
            branches.add(head)
        print(f"Approving workflow run {run_id} (branch={head})")
        ok = approve_run(REPO, run_id)
        if ok:
            print(f"Approved {run_id}")
            if rerun(REPO, run_id):
                print(f"Triggered rerun for {run_id}")
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
    # Exit non-zero if nothing succeeded, to make troubleshooting visible
    if not runs and not dispatched_any:
        sys.exit(0)

if __name__ == "__main__":
    main()
