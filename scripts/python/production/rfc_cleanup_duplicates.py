#!/usr/bin/env python3
"""
RFC Cleanup Duplicates Script

This script implements the logic for cleaning up duplicate RFC PRs.
It finds RFC series with multiple open PRs and keeps only the lowest
numbered micro-issue, cleaning up the rest.

Usage:
    python rfc_cleanup_duplicates.py [--dry-run] [--repo REPO]

Arguments:
    --dry-run: Show what would be done without making changes
    --repo: Repository name (default: from environment)
"""

import argparse
import json
import os
import re
import subprocess
import sys
from typing import Any, Dict, List, Optional


class GitHubAPI:
    """Wrapper for GitHub CLI commands."""

    def __init__(self, repo: str, token: str = None):
        self.repo = repo
        self.token = (
            token or os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
        )

    def run_gh_command(self, args: List[str], capture_output: bool = True) -> str:
        """Run a GitHub CLI command."""
        cmd = ["gh"] + args
        if self.repo:
            cmd.extend(["--repo", self.repo])

        env = os.environ.copy()
        if self.token:
            env["GH_TOKEN"] = self.token

        result = subprocess.run(cmd, capture_output=capture_output, text=True, env=env)

        if result.returncode != 0:
            print(f"❌ Command failed: {' '.join(cmd)}")
            if result.stderr:
                print(f"Error: {result.stderr}")
            return ""

        return result.stdout.strip()

    def get_open_prs(self) -> List[Dict[str, Any]]:
        """Get all open PRs."""
        output = self.run_gh_command(
            ["pr", "list", "--state", "open", "--json", "number,title,headRefName"]
        )

        if not output:
            return []

        try:
            return json.loads(output)
        except json.JSONDecodeError:
            print("❌ Failed to parse PR list")
            return []

    def close_pr(self, pr_number: int, comment: str) -> bool:
        """Close a PR with a comment."""
        result = self.run_gh_command(
            ["pr", "close", str(pr_number), "--comment", comment]
        )
        return bool(result)

    def delete_branch(self, branch_name: str) -> bool:
        """Delete a branch."""
        if branch_name in ["main", "master"]:
            print(f"⚠️  Skipping deletion of protected branch: {branch_name}")
            return True

        self.run_gh_command(
            [
                "api",
                f"repos/{self.repo}/git/refs/heads/{branch_name}",
                "--method",
                "DELETE",
            ],
            capture_output=False,
        )
        return True  # GitHub API returns empty on success

    def get_open_issues(self) -> List[Dict[str, Any]]:
        """Get all open issues."""
        output = self.run_gh_command(
            ["issue", "list", "--state", "open", "--json", "number,title"]
        )

        if not output:
            return []

        try:
            return json.loads(output)
        except json.JSONDecodeError:
            print("❌ Failed to parse issue list")
            return []

    def close_issue(self, issue_number: int, comment: str) -> bool:
        """Close an issue with a comment."""
        result = self.run_gh_command(
            ["issue", "close", str(issue_number), "--comment", comment]
        )
        return bool(result)

    def create_issue(self, title: str, body: str, labels: List[str] = None) -> bool:
        """Create a new issue."""
        cmd = ["issue", "create", "--title", title, "--body", body]
        if labels:
            for label in labels:
                cmd.extend(["--label", label])

        result = self.run_gh_command(cmd)
        return bool(result)


class RFCCleanupLogic:
    """Core logic for RFC cleanup operations."""

    @staticmethod
    def is_rfc_pr(title: str) -> bool:
        """Check if a PR title matches RFC pattern."""
        return bool(re.search(r"RFC-\d{3}-\d{2}", title))

    @staticmethod
    def extract_rfc_info(title: str) -> Optional[Dict[str, int]]:
        """Extract RFC number and micro number from title."""
        match = re.search(r"RFC-(\d{3})-(\d{2})", title)
        if match:
            return {
                "rfc_number": int(match.group(1)),
                "micro_number": int(match.group(2)),
            }
        return None

    @staticmethod
    def find_duplicate_rfcs(prs: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """
        Find RFC series that have multiple open PRs.

        Returns a list of duplicate RFC data with PR information.
        """
        # Filter to RFC PRs only
        rfc_prs = []
        for pr in prs:
            if RFCCleanupLogic.is_rfc_pr(pr["title"]):
                rfc_info = RFCCleanupLogic.extract_rfc_info(pr["title"])
                if rfc_info:
                    rfc_prs.append(
                        {
                            "number": pr["number"],
                            "title": pr["title"],
                            "headRefName": pr.get("headRefName", ""),
                            "rfc_number": rfc_info["rfc_number"],
                            "micro_number": rfc_info["micro_number"],
                        }
                    )

        # Group by RFC number
        rfc_groups = {}
        for pr in rfc_prs:
            rfc_num = pr["rfc_number"]
            if rfc_num not in rfc_groups:
                rfc_groups[rfc_num] = []
            rfc_groups[rfc_num].append(pr)

        # Find groups with multiple PRs
        duplicates = []
        for rfc_num, prs_list in rfc_groups.items():
            if len(prs_list) > 1:
                # Sort by micro number
                sorted_prs = sorted(prs_list, key=lambda x: x["micro_number"])
                duplicates.append({"rfc_number": rfc_num, "prs": sorted_prs})

        return duplicates

    @staticmethod
    def generate_cleanup_actions(
        duplicates: List[Dict[str, Any]]
    ) -> List[Dict[str, Any]]:
        """
        Generate a list of cleanup actions for duplicate RFCs.

        Each action represents an operation to perform.
        """
        actions = []

        for duplicate in duplicates:
            rfc_num = duplicate["rfc_number"]
            prs = duplicate["prs"]

            # Keep the first (lowest micro number) PR
            pr_to_keep = prs[0]
            actions.append(
                {
                    "action": "keep_pr",
                    "pr_number": pr_to_keep["number"],
                    "title": pr_to_keep["title"],
                    "rfc_number": rfc_num,
                    "micro_number": pr_to_keep["micro_number"],
                }
            )

            # Generate cleanup actions for the rest
            for pr in prs[1:]:
                actions.extend(
                    [
                        {
                            "action": "close_pr",
                            "pr_number": pr["number"],
                            "title": pr["title"],
                            "comment": "Closed due to duplicate RFC work. Only one micro-issue per RFC series should be active at a time.",
                        },
                        {
                            "action": "delete_branch",
                            "branch_name": pr["headRefName"],
                            "pr_number": pr["number"],
                        },
                        {
                            "action": "close_issue",
                            "pr_number": pr["number"],
                            "title": pr["title"],
                            "comment": "Closed due to duplicate RFC work. Issue will be recreated without assignment.",
                        },
                        {
                            "action": "recreate_issue",
                            "rfc_number": rfc_num,
                            "micro_number": pr["micro_number"],
                            "title": pr["title"],
                            "body": f'This is a recreated issue for RFC-{rfc_num}-{pr["micro_number"]:02d}. '
                            f"Original issue was closed due to duplicate RFC work. "
                            f"Only one micro-issue per RFC series should be active at a time.",
                        },
                    ]
                )

        return actions


class RFCCleanupRunner:
    """Main runner for RFC cleanup operations."""

    def __init__(self, repo: str, dry_run: bool = False):
        self.repo = repo
        self.dry_run = dry_run
        self.gh = GitHubAPI(repo)
        self.logic = RFCCleanupLogic()

    def run_cleanup(self) -> bool:
        """Run the complete cleanup process."""
        print("🧹 Starting RFC Cleanup Process")
        print(f"Repository: {self.repo}")
        print(f"Dry run: {self.dry_run}")
        print("-" * 50)

        # Get open PRs
        print("\n📋 Fetching open PRs...")
        prs = self.gh.get_open_prs()
        print(f"Found {len(prs)} open PRs")

        if not prs:
            print("✅ No open PRs found")
            return True

        # Find duplicates
        print("\n🔍 Analyzing for duplicate RFCs...")
        duplicates = self.logic.find_duplicate_rfcs(prs)
        print(f"Found {len(duplicates)} RFC series with duplicates")

        if not duplicates:
            print("✅ No duplicate RFCs found")
            return True

        # Generate cleanup actions
        actions = self.logic.generate_cleanup_actions(duplicates)

        print(f"\n📝 Generated {len(actions)} cleanup actions")

        # Execute actions
        success = self._execute_actions(actions)

        print("🎉 Cleanup process completed!")
        return success

    def _execute_actions(self, actions: List[Dict[str, Any]]) -> bool:
        """Execute the cleanup actions."""
        success = True

        for action in actions:
            action_type = action["action"]

            try:
                if action_type == "keep_pr":
                    print(f"✅ Keeping PR #{action['pr_number']}: {action['title']}")

                elif action_type == "close_pr":
                    if self.dry_run:
                        print(
                            f"🔍 Would close PR #{action['pr_number']}: {action['title']}"
                        )
                    else:
                        print(
                            f"📝 Closing PR #{action['pr_number']}: {action['title']}"
                        )
                        if not self.gh.close_pr(action["pr_number"], action["comment"]):
                            print(f"❌ Failed to close PR #{action['pr_number']}")
                            success = False

                elif action_type == "delete_branch":
                    branch_name = action["branch_name"]
                    if self.dry_run:
                        print(f"🔍 Would delete branch: {branch_name}")
                    else:
                        print(f"🗑️  Deleting branch: {branch_name}")
                        if not self.gh.delete_branch(branch_name):
                            print(f"❌ Failed to delete branch: {branch_name}")
                            success = False

                elif action_type == "close_issue":
                    if self.dry_run:
                        print(f"🔍 Would close issue for PR #{action['pr_number']}")
                    else:
                        # Find the corresponding issue
                        issues = self.gh.get_open_issues()
                        issue_number = None
                        for issue in issues:
                            if issue["title"] == action["title"]:
                                issue_number = issue["number"]
                                break

                        if issue_number:
                            print(
                                f"📝 Closing issue #{issue_number}: {action['title']}"
                            )
                            if not self.gh.close_issue(issue_number, action["comment"]):
                                print(f"❌ Failed to close issue #{issue_number}")
                                success = False
                        else:
                            print(
                                f"⚠️  Could not find issue for PR #{action['pr_number']}"
                            )

                elif action_type == "recreate_issue":
                    if self.dry_run:
                        print(f"🔍 Would recreate issue: {action['title']}")
                    else:
                        print(f"🔄 Recreating issue: {action['title']}")
                        labels = [f'rfc-{action["rfc_number"]}']
                        if not self.gh.create_issue(
                            action["title"], action["body"], labels
                        ):
                            print(f"❌ Failed to recreate issue: {action['title']}")
                            success = False

            except Exception as e:
                print(f"❌ Error executing action {action_type}: {e}")
                success = False

        return success


def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="RFC Cleanup Duplicates")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be done without making changes",
    )
    parser.add_argument(
        "--repo",
        default=os.environ.get("GITHUB_REPOSITORY"),
        help="Repository name (default: from GITHUB_REPOSITORY env var)",
    )

    args = parser.parse_args()

    if not args.repo:
        print(
            "❌ Repository not specified. Use --repo or set GITHUB_REPOSITORY environment variable"
        )
        return 1

    runner = RFCCleanupRunner(args.repo, args.dry_run)
    success = runner.run_cleanup()

    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
