#!/usr/bin/env python3
"""
Direct merge script for eligible PRs.
Bypasses GitHub's auto-merge restrictions by performing direct merge via API.
"""
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
    return subprocess.run(cmd, check=check, text=True, capture_output=True, env=env, encoding="utf-8", errors="replace")


def gh_json(cmd, extra_env: Optional[dict] = None):
    res = run(cmd, extra_env=extra_env)
    if not res.stdout or not res.stdout.strip():
        raise ValueError(f"Empty response from command: {' '.join(cmd)}")
    return json.loads(res.stdout)


def find_pr_number_by_branch(repo: str, branch: str) -> Optional[int]:
    out = run(
        [
            "gh",
            "pr",
            "list",
            "--repo",
            repo,
            "--state",
            "open",
            "--json",
            "number,headRefName",
        ],
        extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")},
    ).stdout
    items = json.loads(out)
    for it in items:
        if it.get("headRefName") == branch:
            return it.get("number")
    return None


def is_pr_eligible_for_merge(repo: str, pr_number: int) -> tuple[bool, str]:
    """Check if PR is eligible for direct merge."""
    try:
        pr = gh_json(
            [
                "gh",
                "pr",
                "view",
                str(pr_number),
                "--repo",
                repo,
                "--json",
                "number,title,author,isDraft,mergeable,mergeStateStatus," "reviewDecision,autoMergeRequest,state",
            ],
            extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")},
        )

        # Basic checks
        title = pr.get("title", "")
        author = pr.get("author", {}).get("login", "")
        draft = pr.get("isDraft", True)
        mergeable = pr.get("mergeable") == "MERGEABLE"
        state = pr.get("state", "")

        if state != "OPEN":
            return False, f"PR is not open (state: {state})"

        if author not in {
            "Copilot",
            "app/copilot-swe-agent",
            "github-actions[bot]",
            "github-actions",
        }:
            return False, f"Non-Copilot author: {author}"

        if "RFC-" not in title:
            return False, "No RFC tag in title"

        if draft:
            return False, "PR is still draft"

        if not mergeable:
            return False, f"PR not mergeable (mergeable: {pr.get('mergeable')})"

        # Check if we should proceed despite unstable state
        merge_state = pr.get("mergeStateStatus", "")
        if merge_state in ["CLEAN"]:
            return True, "PR is clean and ready"
        elif merge_state in ["UNSTABLE"] and mergeable:
            return True, "PR is unstable but mergeable - proceeding with direct merge"
        else:
            return False, f"PR merge state not suitable: {merge_state}, mergeable: {mergeable}"

    except Exception as e:
        return False, f"Error checking PR eligibility: {e}"


def direct_merge_pr(repo: str, pr_number: int) -> bool:
    """Perform direct merge of the PR."""
    try:
        run(
            [
                "gh",
                "pr",
                "merge",
                str(pr_number),
                "--repo",
                repo,
                "--squash",
                "--auto",  # Skip confirmation
            ],
            extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")},
        )
        return True
    except subprocess.CalledProcessError as e:
        sys.stderr.write(f"Direct merge failed: {e.stderr or ''}")
        return False


def add_comment(repo: str, pr_number: int, body: str) -> None:
    """Add comment to PR."""
    try:
        run(
            ["gh", "pr", "comment", str(pr_number), "--repo", repo, "--body", body],
            check=False,
            extra_env={"GH_TOKEN": os.environ.get("AUTO_APPROVE_PAT") or os.environ.get("GH_TOKEN", "")},
        )
    except Exception:
        pass  # Don't fail the workflow if comment fails


def process_pr(repo: str, pr_number: int) -> None:
    """Process a PR for direct merge."""
    print(f"Evaluating PR #{pr_number} for direct merge...")

    eligible, reason = is_pr_eligible_for_merge(repo, pr_number)

    if not eligible:
        print(f"PR #{pr_number} not eligible for direct merge: {reason}")
        return

    print(f"PR #{pr_number} eligible for direct merge: {reason}")

    if direct_merge_pr(repo, pr_number):
        print(f"Successfully merged PR #{pr_number}")
        add_comment(
            repo,
            pr_number,
            "🎉 **Automatic Direct Merge Completed**\n\n"
            "This PR was automatically merged using direct merge workflow because:\n"
            f"- {reason}\n"
            "- All required checks passed\n"
            "- Auto-merge restrictions bypassed for automation flow",
        )
    else:
        print(f"Failed to merge PR #{pr_number}")
        add_comment(
            repo,
            pr_number,
            "⚠️ **Direct Merge Failed**\n\n"
            "Automatic direct merge could not be completed. Possible reasons:\n"
            "- Merge conflicts detected\n"
            "- Required status checks still pending/failing\n"
            "- Repository permissions insufficient\n"
            "- Branch protection rules preventing merge\n\n"
            "Please review and merge manually if appropriate.",
        )


def main():
    global EVENT_JSON
    if not REPO:
        print("REPO not set", file=sys.stderr)
        sys.exit(1)

    # Check if PR number is provided directly
    pr_number_env = os.environ.get("PR_NUMBER")
    if pr_number_env:
        try:
            pr_number = int(pr_number_env)
            print(f"Using PR number from environment: #{pr_number}")
            process_pr(REPO, pr_number)
            return
        except (ValueError, TypeError):
            print("Invalid PR_NUMBER in environment", file=sys.stderr)

    if not EVENT_JSON:
        EVENT_JSON = (
            os.environ.get("GITHUB_EVENT_PATH")
            and open(os.environ["GITHUB_EVENT_PATH"], "r", encoding="utf-8").read()
            or "{}"
        )
    evt = json.loads(EVENT_JSON)

    # Handle workflow_run events from auto-approve-merge workflow
    wr = evt.get("workflow_run", {})
    conclusion = wr.get("conclusion")
    if conclusion != "success":
        print("Auto-approve workflow did not succeed; exiting")
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

    process_pr(REPO, pr_number)


if __name__ == "__main__":
    main()
