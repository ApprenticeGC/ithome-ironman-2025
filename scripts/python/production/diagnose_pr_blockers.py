#!/usr/bin/env python3
"""
Auto-merge diagnostics script
Provides detailed analysis of what's blocking PR auto-merge
Integrates with existing automation pipeline
"""

import json
import subprocess
import sys
from typing import Dict, List


def run_command(cmd: List[str]) -> subprocess.CompletedProcess:
    """Run command with proper Unicode handling."""
    return subprocess.run(cmd, text=True, capture_output=True, encoding="utf-8", errors="replace")


def get_pr_diagnostics(repo: str, pr_number: str) -> Dict:
    """Get comprehensive PR diagnostics using GraphQL."""
    query = """
    query($owner: String!, $name: String!, $number: Int!) {
      repository(owner: $owner, name: $name) {
        pullRequest(number: $number) {
          id number isDraft state mergeable mergeStateStatus isInMergeQueue
          baseRefName headRefName
          reviewDecision
          reviewThreads(first: 100) {
            nodes { isResolved }
          }
          commits(last: 1) {
            nodes {
              commit {
                oid
                statusCheckRollup {
                  state
                  contexts(first: 50) {
                    nodes {
                      __typename
                      ... on CheckRun {
                        name status conclusion detailsUrl
                      }
                      ... on StatusContext {
                        context state targetUrl
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    """

    owner, name = repo.split("/")
    result = run_command(
        [
            "gh",
            "api",
            "graphql",
            "-f",
            f"query={query}",
            "-F",
            f"owner={owner}",
            "-F",
            f"name={name}",
            "-F",
            f"number={pr_number}",
        ]
    )

    if result.returncode != 0:
        raise Exception(f"GraphQL query failed: {result.stderr}")

    return json.loads(result.stdout)


def get_check_runs(repo: str, sha: str) -> Dict:
    """Get detailed check-runs for a commit."""
    result = run_command(
        ["gh", "api", f"repos/{repo}/commits/{sha}/check-runs", "-H", "Accept: application/vnd.github+json"]
    )

    if result.returncode != 0:
        return {"check_runs": []}

    return json.loads(result.stdout)


def get_commit_statuses(repo: str, sha: str) -> List[Dict]:
    """Get commit statuses (old API)."""
    result = run_command(["gh", "api", f"repos/{repo}/statuses/{sha}"])

    if result.returncode != 0:
        return []

    return json.loads(result.stdout)


def get_branch_comparison(repo: str, base: str, head: str) -> Dict:
    """Get branch comparison to check if behind."""
    result = run_command(["gh", "api", f"repos/{repo}/compare/{base}...{head}"])

    if result.returncode != 0:
        return {"behind_by": 0}

    return json.loads(result.stdout)


def analyze_pr_blockers(repo: str, pr_number: str) -> Dict:
    """Analyze what's blocking a PR from auto-merge."""
    print(f"Analyzing PR #{pr_number} for auto-merge blockers...")

    # Get comprehensive PR data
    pr_data = get_pr_diagnostics(repo, pr_number)
    pr = pr_data["data"]["repository"]["pullRequest"]

    # Extract key information
    head_sha = pr["commits"]["nodes"][0]["commit"]["oid"]
    analysis = {
        "pr_number": pr_number,
        "head_sha": head_sha,
        "is_draft": pr["isDraft"],
        "state": pr["state"],
        "mergeable": pr["mergeable"],
        "merge_state": pr["mergeStateStatus"],
        "review_decision": pr["reviewDecision"],
        "base_ref": pr["baseRefName"],
        "head_ref": pr["headRefName"],
        "blockers": [],
        "recommendations": [],
    }

    # Check for unresolved review threads
    unresolved_threads = len([thread for thread in pr["reviewThreads"]["nodes"] if not thread["isResolved"]])
    analysis["unresolved_threads"] = unresolved_threads

    # Get check status
    status_rollup = pr["commits"]["nodes"][0]["commit"]["statusCheckRollup"]
    analysis["check_state"] = status_rollup["state"] if status_rollup else "UNKNOWN"

    # Get detailed check runs
    check_runs = get_check_runs(repo, head_sha)
    failing_checks = []
    pending_checks = []

    for check in check_runs.get("check_runs", []):
        if check["status"] != "completed":
            pending_checks.append(
                {"name": check["name"], "status": check["status"], "conclusion": check.get("conclusion")}
            )
        elif check["conclusion"] not in ["success", "neutral", "skipped"]:
            failing_checks.append(
                {
                    "name": check["name"],
                    "status": check["status"],
                    "conclusion": check["conclusion"],
                    "details_url": check.get("detailsUrl"),
                }
            )

    analysis["pending_checks"] = pending_checks
    analysis["failing_checks"] = failing_checks

    # Get commit statuses (legacy)
    statuses = get_commit_statuses(repo, head_sha)
    failing_statuses = [s for s in statuses if s["state"] != "success"]
    analysis["failing_statuses"] = failing_statuses

    # Check if branch is behind
    comparison = get_branch_comparison(repo, analysis["base_ref"], analysis["head_ref"])
    analysis["behind_by"] = comparison.get("behind_by", 0)

    # Analyze blockers
    if analysis["is_draft"]:
        analysis["blockers"].append("PR is still draft")
        analysis["recommendations"].append("Mark PR as ready for review")

    if analysis["review_decision"] == "CHANGES_REQUESTED":
        analysis["blockers"].append("Reviewer requested changes")
        analysis["recommendations"].append("Address requested changes and re-request review")

    if analysis["review_decision"] == "REVIEW_REQUIRED":
        analysis["blockers"].append("Approval required")
        analysis["recommendations"].append("Get required reviewers to approve")

    if unresolved_threads > 0:
        analysis["blockers"].append(f"{unresolved_threads} unresolved review threads")
        analysis["recommendations"].append("Resolve all conversation threads")

    if pending_checks or failing_checks or failing_statuses:
        analysis["blockers"].append("Checks not passing")
        analysis["recommendations"].append("Wait for checks to complete successfully")

    if analysis["behind_by"] > 0:
        analysis["blockers"].append(f"Branch is {analysis['behind_by']} commits behind base")
        analysis["recommendations"].append("Update branch with latest changes from base")

    # Special case: likely CODEOWNERS issue
    if (
        analysis["review_decision"] != "APPROVED"
        and unresolved_threads == 0
        and not failing_checks
        and not failing_statuses
    ):
        analysis["blockers"].append("Likely missing CODEOWNERS approval")
        analysis["recommendations"].append("Check if code owners need to approve changes")

    return analysis


def format_diagnostic_report(analysis: Dict) -> str:
    """Format the analysis into a readable report."""
    pr_num = analysis["pr_number"]

    report = f"""
### Auto-merge Diagnostics for PR #{pr_num}

**Status:** `{analysis['merge_state']}` | **Mergeable:** `{analysis['mergeable']}`
**Draft:** `{analysis['is_draft']}` | **Review Decision:** `{analysis['review_decision']}`
**Check State:** `{analysis['check_state']}` | **Behind Base:** {analysis['behind_by']} commits
**Unresolved Threads:** {analysis['unresolved_threads']}

#### Blockers Preventing Auto-merge:
"""

    if analysis["blockers"]:
        for blocker in analysis["blockers"]:
            report += f"- ❌ {blocker}\n"
    else:
        report += "- ✅ No blockers detected!\n"

    report += "\n#### Recommendations:\n"
    if analysis["recommendations"]:
        for rec in analysis["recommendations"]:
            report += f"- 📋 {rec}\n"
    else:
        report += "- ✅ Ready for auto-merge!\n"

    if analysis["failing_checks"]:
        report += f"\n#### Failing Checks ({len(analysis['failing_checks'])}):\n"
        for check in analysis["failing_checks"]:
            report += f"- ❌ {check['name']}: {check['conclusion']}\n"

    if analysis["pending_checks"]:
        report += f"\n#### Pending Checks ({len(analysis['pending_checks'])}):\n"
        for check in analysis["pending_checks"]:
            report += f"- ⏳ {check['name']}: {check['status']}\n"

    return report


def can_auto_merge(analysis: Dict) -> bool:
    """Determine if PR can be auto-merged based on analysis."""
    return (
        not analysis["is_draft"]
        and analysis["review_decision"] == "APPROVED"
        and analysis["unresolved_threads"] == 0
        and not analysis["failing_checks"]
        and not analysis["failing_statuses"]
        and analysis["check_state"] in ["SUCCESS", "EXPECTED"]
        and analysis["mergeable"] == "MERGEABLE"
    )


def main():
    """Main diagnostic logic."""
    if len(sys.argv) < 3:
        print("Usage: python diagnose_pr.py <repo> <pr_number>")
        sys.exit(1)

    repo = sys.argv[1]
    pr_number = sys.argv[2]

    try:
        analysis = analyze_pr_blockers(repo, pr_number)
        report = format_diagnostic_report(analysis)

        print(report)

        # Return appropriate exit code
        if can_auto_merge(analysis):
            print(f"\n✅ PR #{pr_number} is ready for auto-merge!")
            sys.exit(0)
        else:
            print(f"\n❌ PR #{pr_number} has blockers preventing auto-merge")
            sys.exit(1)

    except Exception as e:
        print(f"Error analyzing PR #{pr_number}: {e}")
        sys.exit(2)


if __name__ == "__main__":
    main()
