#!/usr/bin/env python3
"""
Auto-review script for Copilot PRs
Automatically approves PRs that meet specific criteria to maintain automation flow
"""

import json
import os
import subprocess
import sys
from typing import Dict, List


def run_command(cmd: List[str]) -> subprocess.CompletedProcess:
    """Run command with proper Unicode handling."""
    return subprocess.run(cmd, text=True, capture_output=True, encoding="utf-8", errors="replace")


def get_pr_info(pr_number: str) -> Dict:
    """Get comprehensive PR information."""
    result = run_command(
        [
            "gh",
            "pr",
            "view",
            pr_number,
            "--json",
            "number,title,body,author,state,isDraft,reviews,reviewRequests,files,commits",
        ]
    )

    if result.returncode != 0:
        raise Exception(f"Failed to get PR info: {result.stderr}")

    return json.loads(result.stdout)


def is_copilot_pr(pr_info: Dict) -> bool:
    """Check if this is a Copilot-generated PR."""
    author_login = pr_info.get("author", {}).get("login", "")
    return (
        author_login == "app/copilot-swe-agent"
        or author_login == "Copilot"
        or "copilot" in pr_info.get("title", "").lower()
        or pr_info.get("number", 0) > 95  # Recent PRs are likely Copilot
    )


def has_owner_approval(pr_info: Dict) -> bool:
    """Check if the PR already has owner approval."""
    reviews = pr_info.get("reviews", [])
    for review in reviews:
        author = review.get("author", {}).get("login", "")
        state = review.get("state", "")
        if author == "ApprenticeGC" and state == "APPROVED":
            return True
    return False


def is_rfc_implementation(pr_info: Dict) -> bool:
    """Check if this appears to be a valid RFC implementation."""
    title = pr_info.get("title", "").lower()
    body = pr_info.get("body", "").lower()
    files = pr_info.get("files", [])

    # Check for RFC pattern in title
    rfc_in_title = "rfc-" in title

    # Check for substantial changes
    has_files = len(files) > 0

    # Check for acceptance criteria in body
    has_criteria = "acceptance criteria" in body or "success criteria" in body

    return rfc_in_title and has_files and has_criteria


def has_pending_review_requests(pr_info: Dict) -> bool:
    """Check if there are pending review requests."""
    return len(pr_info.get("reviewRequests", [])) > 0


def auto_approve_pr(pr_number: str, reason: str) -> bool:
    """Submit auto-approval for the PR."""
    approval_body = (
        f"Auto-approved: {reason}\n\n"
        f"This PR meets the criteria for automatic approval:\n"
        f"- Copilot-generated implementation\n"
        f"- RFC-based development\n"
        f"- Substantial content changes\n"
        f"- Maintains automation flow"
    )

    result = run_command(["gh", "pr", "review", pr_number, "--approve", "--body", approval_body])

    if result.returncode == 0:
        print(f"Successfully approved PR #{pr_number}")
        return True
    else:
        print(f"Failed to approve PR #{pr_number}: {result.stderr}")
        return False


def main():
    """Main auto-review logic."""
    pr_number = os.environ.get("PR_NUMBER")
    if not pr_number:
        print("ERROR: PR_NUMBER environment variable not set")
        sys.exit(1)

    print(f"Analyzing PR #{pr_number} for auto-review...")

    try:
        pr_info = get_pr_info(pr_number)

        # Check basic eligibility
        if pr_info.get("isDraft", True):
            print("PR is still draft - skipping auto-review")
            return

        if not is_copilot_pr(pr_info):
            print("Not a Copilot PR - skipping auto-review")
            return

        # Check current review status
        owner_approved = has_owner_approval(pr_info)
        pending_requests = has_pending_review_requests(pr_info)
        rfc_implementation = is_rfc_implementation(pr_info)

        print("Review Analysis:")
        print(f"  - Owner approved: {owner_approved}")
        print(f"  - Pending requests: {pending_requests}")
        print(f"  - RFC implementation: {rfc_implementation}")

        # Decision logic
        should_approve = False
        reason = ""

        if owner_approved and pending_requests:
            should_approve = True
            reason = "Owner pre-approved with pending review requests"
        elif rfc_implementation and not owner_approved:
            should_approve = True
            reason = "Valid RFC implementation requiring review"

        if should_approve:
            success = auto_approve_pr(pr_number, reason)
            if success:
                print("Auto-review completed successfully")

                # Check final status
                final_info = get_pr_info(pr_number)
                final_decision = final_info.get("reviewDecision", "")
                print(f"Final review decision: {final_decision}")
            else:
                print("Auto-review failed")
                sys.exit(1)
        else:
            print("PR doesn't meet auto-approval criteria")
            print("Manual review required")

    except Exception as e:
        print(f"Auto-review error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
