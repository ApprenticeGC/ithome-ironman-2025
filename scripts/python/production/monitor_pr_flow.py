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
import re
import subprocess
import sys
from typing import List, Optional

REPO = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY", "")


def run(cmd: List[str], check: bool = True, extra_env: Optional[dict] = None):
    env = os.environ.copy()
    if extra_env:
        env.update(extra_env)
    return subprocess.run(
        cmd,
        check=check,
        text=True,
        capture_output=True,
        env=env,
        encoding="utf-8",
        errors="replace",
    )


def gh_json(cmd: List[str], extra_env: Optional[dict] = None):
    res = run(cmd, extra_env=extra_env)
    if not res.stdout or not res.stdout.strip():
        raise ValueError(f"Empty response from command: {' '.join(cmd)}")
    return json.loads(res.stdout)


def resolve_commit_sha(repo: str, ref: str, extra_env: Optional[dict] = None) -> Optional[str]:
    """Resolve a branch, tag, or SHA to a commit OID."""
    if not ref:
        return None
    ref = ref.strip()
    if not ref:
        return None
    if "/" not in repo:
        return None
    owner, name = repo.split("/", 1)
    candidates = [ref]
    if ref.startswith("refs/"):
        heads = ref.removeprefix("refs/heads/")
        tags = ref.removeprefix("refs/tags/")
        if heads != ref:
            candidates.append(heads)
        if tags != ref:
            candidates.append(tags)
    else:
        candidates.extend([f"refs/heads/{ref}", f"refs/tags/{ref}"])

    env_for_gh = extra_env or None
    for candidate in candidates:
        if not candidate:
            continue
        result = run(
            [
                "gh",
                "api",
                "graphql",
                "-F",
                f"owner={owner}",
                "-F",
                f"name={name}",
                "-F",
                f"expression={candidate}",
                "-f",
                (
                    "query=query($owner:String!,$name:String!,$expression:String!)"
                    "{repository(owner:$owner,name:$name){object(expression:$expression){... on Commit { oid }}}}"
                ),
                "--jq",
                ".data.repository.object.oid // empty",
            ],
            check=False,
            extra_env=env_for_gh,
        )
        if result.returncode == 0 and result.stdout.strip():
            return result.stdout.strip()

    if re.fullmatch(r"[0-9a-f]{40}", ref):
        return ref

    return None


def list_open_copilot_prs(repo: str):
    # Use REST API to avoid gh pr list flakiness; paginate for completeness
    page = 1
    found = []
    while True:
        try:
            # Correct pulls list endpoint with query params
            prs = gh_json(["gh", "api", f"repos/{repo}/pulls?state=open&per_page=100&page={page}"])
        except subprocess.CalledProcessError as e:
            sys.stderr.write(f"list_open_copilot_prs error: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
            break
        if not isinstance(prs, list) or not prs:
            break
        for pr in prs:
            head = (pr.get("head") or {}).get("ref", "")
            base_repo = (pr.get("base") or {}).get("repo") or {}
            base_full = base_repo.get("full_name", "")
            # Target Copilot-authored branches in this repo; do not hard-require RFC token
            if head.startswith("copilot/") and base_full == repo:
                found.append(
                    {
                        "number": pr.get("number"),
                        "headRefName": head,
                        "isDraft": pr.get("draft", True),
                        "autoMergeRequest": pr.get("auto_merge"),
                    }
                )
        page += 1
    return found


def has_success_ci(repo: str, branch: str) -> bool:
    """Return True if either 'ci' or 'ci-dispatch' has a successful run on branch."""
    try:
        # Use check-runs API to look for successful build_test runs
        try:
            result = run(
                [
                    "gh",
                    "api",
                    f"repos/{repo}/commits/{branch}/check-runs",
                    "--jq",
                    '.check_runs[] | select(.name == "build_test" and .conclusion == "success") | .id',
                ]
            )

            # If we got any successful build_test check runs, CI is good
            if result.stdout and result.stdout.strip():
                return True
        except subprocess.CalledProcessError:
            pass  # Fall through to workflow runs check

        # Fallback: check recent workflow runs
        for workflow_name in ["ci", "ci-dispatch"]:
            try:
                result = run(
                    [
                        "gh",
                        "run",
                        "list",
                        "--repo",
                        repo,
                        "--workflow",
                        workflow_name,
                        "--branch",
                        branch,
                        "--limit",
                        "5",
                        "--json",
                        "conclusion,headSha",
                    ]
                )

                if result.stdout and result.stdout.strip():
                    import json

                    runs = json.loads(result.stdout)
                    for run_item in runs:
                        if run_item.get("conclusion") == "success":
                            return True
            except subprocess.CalledProcessError:
                continue  # Try next workflow

        return False

    except subprocess.CalledProcessError as e:
        sys.stderr.write(
            f"has_success_ci error for {branch}: {e}\n"
            f"STDOUT: {getattr(e, 'stdout', 'N/A')}\n"
            f"STDERR: {getattr(e, 'stderr', 'N/A')}\n"
        )
        return False


def has_recent_ci_activity(repo: str, branch: str) -> bool:
    """Check if there are recent CI runs (pending, running, or successful) to avoid duplicate dispatches."""
    try:
        from datetime import datetime, timedelta

        cutoff_time = datetime.utcnow() - timedelta(minutes=10)  # Look back 10 minutes

        for workflow_name in ["ci", "ci-dispatch"]:
            try:
                result = run(
                    [
                        "gh",
                        "run",
                        "list",
                        "--repo",
                        repo,
                        "--workflow",
                        workflow_name,
                        "--branch",
                        branch,
                        "--limit",
                        "5",
                        "--json",
                        "conclusion,status,createdAt",
                    ]
                )

                if result.stdout.strip():
                    import json

                    runs = json.loads(result.stdout)
                    for run_item in runs:
                        created_at = datetime.fromisoformat(run_item.get("createdAt", "").replace("Z", "+00:00"))
                        if created_at > cutoff_time:
                            status = (run_item.get("status") or "").lower()
                            conclusion = (run_item.get("conclusion") or "").lower()
                            active_statuses = {"queued", "pending", "in_progress", "waiting", "requested"}
                            if status in active_statuses or conclusion in {"success", "action_required"}:
                                return True
            except (subprocess.CalledProcessError, ValueError):
                continue

        return False
    except Exception:
        return False  # If we can't determine, err on the side of not dispatching


def dispatch_ci(repo: str, branch: str) -> bool:
    # Check if there's already recent CI activity to avoid duplicates
    if has_recent_ci_activity(repo, branch):
        print(f"Skipping CI dispatch for {branch} - recent CI activity detected")
        return True  # Treat as success since CI is already handled

    # Prefer PAT if available
    env_pat = {}
    pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
    if pat:
        env_pat["GH_TOKEN"] = pat

    resolved_sha = resolve_commit_sha(repo, branch, env_pat if env_pat else None)
    if not resolved_sha:
        sys.stderr.write(f"dispatch_ci: unable to resolve {branch} to a commit; skipping CI dispatch.\n")
        return False

    try:
        run(
            ["gh", "workflow", "run", "ci", "--ref", resolved_sha],
            check=True,
            extra_env=env_pat,
        )
        return True
    except subprocess.CalledProcessError as e1:
        sys.stderr.write(
            f"dispatch_ci name error for {branch}: {e1}\n"
            f"STDOUT: {getattr(e1, 'stdout', 'N/A')}\n"
            f"STDERR: {getattr(e1, 'stderr', 'N/A')}\n"
        )
        try:
            wfs = gh_json(
                ["gh", "workflow", "list", "--repo", repo, "--json", "name,id"],
                extra_env=env_pat,
            )
            ci_id = next((str(wf["id"]) for wf in wfs if wf.get("name") == "ci"), None)
            if ci_id:
                run(
                    ["gh", "workflow", "run", ci_id, "--ref", resolved_sha],
                    check=True,
                    extra_env=env_pat,
                )
                return True
        except subprocess.CalledProcessError as e2:
            sys.stderr.write(
                f"dispatch_ci id error for {branch}: {e2}\n"
                f"STDOUT: {getattr(e2, 'stdout', 'N/A')}\n"
                f"STDERR: {getattr(e2, 'stderr', 'N/A')}\n"
            )

    # Fallback to generic dispatcher with input
    try:
        run(
            ["gh", "workflow", "run", "ci-dispatch", "-f", f"target_ref={resolved_sha}"],
            check=True,
            extra_env=env_pat,
        )
        return True
    except subprocess.CalledProcessError as e3:
        sys.stderr.write(
            f"dispatch_ci fallback error for {branch}: {e3}\n"
            f"STDOUT: {getattr(e3, 'stdout', 'N/A')}\n"
            f"STDERR: {getattr(e3, 'stderr', 'N/A')}\n"
        )
    return False


def mark_ready(repo: str, pr_number: int) -> bool:
    try:
        env_pat = {}
        pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
        if pat:
            env_pat["GH_TOKEN"] = pat
        pr_id = run(
            [
                "gh",
                "pr",
                "view",
                str(pr_number),
                "--repo",
                repo,
                "--json",
                "id",
                "-q",
                ".id",
            ],
            extra_env=env_pat,
        ).stdout.strip()
        run(
            [
                "gh",
                "api",
                "graphql",
                "-f",
                "query=mutation($id: ID!) { "
                "markPullRequestReadyForReview(input: {pullRequestId: $id}) "
                "{ clientMutationId } }",
                "-F",
                f"id={pr_id}",
            ],
            check=True,
            extra_env=env_pat,
        )
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
        pr_id = run(
            [
                "gh",
                "pr",
                "view",
                str(pr_number),
                "--repo",
                repo,
                "--json",
                "id",
                "-q",
                ".id",
            ],
            extra_env=env_pat,
        ).stdout.strip()
        query = (
            "mutation($id: ID!, $method: PullRequestMergeMethod!) { "
            "enablePullRequestAutoMerge(input: {pullRequestId: $id, mergeMethod: $method}) { clientMutationId } }"
        )
        run(
            [
                "gh",
                "api",
                "graphql",
                "-f",
                f"query={query}",
                "-F",
                f"id={pr_id}",
                "-F",
                "method=SQUASH",
            ],
            check=True,
            extra_env=env_pat,
        )
        return True
    except subprocess.CalledProcessError as e:
        sys.stderr.write(f"enable_automerge error for PR #{pr_number}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
        return False


def try_merge_now(repo: str, pr_number: int) -> bool:
    """Attempt an immediate merge (squash) as a fallback when auto-merge cannot be enabled."""
    try:
        env_pat = {}
        pat = os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN")
        if pat:
            env_pat["GH_TOKEN"] = pat
        # Merge immediately with squash; delete branch after merge
        run(
            [
                "gh",
                "pr",
                "merge",
                str(pr_number),
                "--repo",
                repo,
                "--squash",
                "--delete-branch",
            ],
            check=True,
            extra_env=env_pat,
        )
        return True
    except subprocess.CalledProcessError as e:
        sys.stderr.write(f"try_merge_now error for PR #{pr_number}: {e}\nSTDOUT: {e.stdout}\nSTDERR: {e.stderr}\n")
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
            ci_succeeded = has_success_ci(REPO, branch)
            if not ci_succeeded:
                if is_draft:
                    print(f"Skipping CI dispatch for draft PR {branch}")
                    continue
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
                    # Fallback: try to merge immediately when auto-merge
                    # cannot be enabled (e.g., no branch protection rules)
                    if try_merge_now(REPO, number):
                        print(f"Merged PR #{number} directly (fallback)")
                    else:
                        print(f"Failed to enable auto-merge or merge PR #{number}")
    except Exception as e:
        # Never fail this monitor; just log the issue
        sys.stderr.write(f"monitor_pr_flow unexpected error: {e}\n")


if __name__ == "__main__":
    main()
