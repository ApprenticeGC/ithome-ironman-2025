#!/usr/bin/env python3
"""
Update Project Status Script

Updates GitHub Project v2 item status based on issue assignment events.
Supports moving items between: Backlog, Ready, In progress, In review, Done
"""

import argparse
import os
import sys
from typing import Dict, List, Optional

import requests


class GitHubProjectUpdater:
    def __init__(self, token: str, owner: str, repo: str):
        """Initialize the GitHub Project updater.

        Args:
            token: GitHub Personal Access Token with project and repo scopes
            owner: Repository owner
            repo: Repository name
        """
        self.token = token
        self.owner = owner
        self.repo = repo
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "Content-Type": "application/json",
            "X-GitHub-Api-Version": "2022-11-28",
        }
        self.graphql_url = "https://api.github.com/graphql"
        self.rest_url = "https://api.github.com"

    def execute_graphql(self, query: str, variables: Optional[Dict] = None) -> Dict:
        """Execute a GraphQL query against GitHub API.

        Args:
            query: GraphQL query string
            variables: Query variables

        Returns:
            GraphQL response data

        Raises:
            Exception: If GraphQL request fails
        """
        payload = {"query": query}
        if variables:
            payload["variables"] = variables

        response = requests.post(self.graphql_url, headers=self.headers, json=payload)

        if response.status_code != 200:
            raise Exception(f"GraphQL request failed: {response.status_code} - {response.text}")

        result = response.json()

        if "errors" in result:
            raise Exception(f"GraphQL errors: {result['errors']}")

        return result["data"]

    def add_issue_comment(self, issue_number: int, comment: str) -> None:
        """Add a comment to an issue.

        Args:
            issue_number: Issue number
            comment: Comment text
        """
        url = f"{self.rest_url}/repos/{self.owner}/{self.repo}/issues/{issue_number}/comments"
        payload = {"body": comment}

        response = requests.post(url, headers=self.headers, json=payload)

        if response.status_code not in [200, 201]:
            print(f"⚠️ Failed to add comment to issue #{issue_number}: {response.status_code}")

    def get_issue_project_items(self, issue_number: int) -> Dict:
        """Get issue details including project items.

        Args:
            issue_number: Issue number

        Returns:
            Issue data with project items
        """
        query = """
        query($owner: String!, $repo: String!, $number: Int!) {
          repository(owner: $owner, name: $repo) {
            issue(number: $number) {
              id
              title
              projectItems(first: 10) {
                nodes {
                  id
                  project {
                    id
                    title
                    number
                  }
                }
              }
            }
          }
        }
        """

        variables = {"owner": self.owner, "repo": self.repo, "number": issue_number}

        data = self.execute_graphql(query, variables)
        return data["repository"]["issue"]

    def get_project_status_field(self, project_id: str) -> Dict:
        """Get project Status field details.

        Args:
            project_id: Project GraphQL node ID

        Returns:
            Status field with options
        """
        query = """
        query($projectId: ID!) {
          node(id: $projectId) {
            ... on ProjectV2 {
              fields(first: 20) {
                nodes {
                  ... on ProjectV2SingleSelectField {
                    id
                    name
                    options {
                      id
                      name
                    }
                  }
                }
              }
            }
          }
        }
        """

        variables = {"projectId": project_id}
        data = self.execute_graphql(query, variables)

        # Find Status field
        fields = data["node"]["fields"]["nodes"]
        status_field = next((field for field in fields if field["name"] == "Status"), None)

        if not status_field:
            raise Exception("No Status field found in project")

        return status_field

    def update_project_item_status(self, project_id: str, item_id: str, field_id: str, option_id: str) -> None:
        """Update project item status field.

        Args:
            project_id: Project GraphQL node ID
            item_id: Project item ID
            field_id: Status field ID
            option_id: Status option ID
        """
        mutation = """
        mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
          updateProjectV2ItemFieldValue(
            input: {
              projectId: $projectId
              itemId: $itemId
              fieldId: $fieldId
              value: {
                singleSelectOptionId: $optionId
              }
            }
          ) {
            projectV2Item {
              id
            }
          }
        }
        """

        variables = {"projectId": project_id, "itemId": item_id, "fieldId": field_id, "optionId": option_id}

        self.execute_graphql(mutation, variables)

    def determine_target_status(self, action: str, assignees: List[str]) -> Optional[str]:
        """Determine target status based on assignment action.

        Args:
            action: Assignment action ('assigned' or 'unassigned')
            assignees: List of assignee logins

        Returns:
            Target status or None if no change needed
        """
        if action == "assigned" and len(assignees) > 0:
            return "Ready"
        elif action == "unassigned" and len(assignees) == 0:
            return "Backlog"
        else:
            return None

    def update_issue_status(self, issue_number: int, action: str, assignees: List[str]) -> None:
        """Update project status for an issue based on assignment.

        Args:
            issue_number: Issue number
            action: Assignment action
            assignees: List of assignee logins
        """
        print(f"🔍 Processing {action} event for issue #{issue_number}")
        print(f"📋 Current assignees: {', '.join(assignees) if assignees else 'none'}")

        # Determine target status
        target_status = self.determine_target_status(action, assignees)

        if not target_status:
            print("ℹ️ No status change needed")
            return

        print(f"🎯 Target status: {target_status}")

        try:
            # Get issue and project items
            issue = self.get_issue_project_items(issue_number)
            print(f"✅ Found issue: {issue['title']}")

            project_items = issue["projectItems"]["nodes"]

            if not project_items:
                print("ℹ️ Issue is not in any projects, skipping status update")
                return

            # Update status in each project
            for project_item in project_items:
                project = project_item["project"]
                print(f"📊 Processing project: {project['title']} (#{project['number']})")

                try:
                    # Get Status field
                    status_field = self.get_project_status_field(project["id"])

                    # Find target status option
                    status_option = next((opt for opt in status_field["options"] if opt["name"] == target_status), None)

                    if not status_option:
                        available_options = [opt["name"] for opt in status_field["options"]]
                        print(f"⚠️ Status option '{target_status}' not found in project {project['title']}")
                        print(f"Available options: {', '.join(available_options)}")
                        continue

                    print(f"🎯 Found status option: {status_option['name']} ({status_option['id']})")

                    # Update project item status
                    self.update_project_item_status(
                        project["id"], project_item["id"], status_field["id"], status_option["id"]
                    )

                    print(f"✅ Updated project {project['title']}: Status set to '{target_status}'")

                    # Add comment to issue
                    comment = (
                        f"🤖 **Project Status Update**\n\n"
                        f"📊 **Project**: {project['title']}\n"
                        f"🔄 **Status**: {target_status}\n"
                        f"⚡ **Trigger**: Issue {action}\n\n"
                        f"_Automated by GitHub Actions workflow._"
                    )

                    self.add_issue_comment(issue_number, comment)
                    print(f"💬 Added status update comment to issue #{issue_number}")

                except Exception as e:
                    print(f"❌ Error updating project {project['title']}: {e}")
                    continue

        except Exception as e:
            print(f"❌ Error updating project status: {e}")

            # Add error comment to issue
            error_comment = (
                f"❌ **Project Status Update Failed**\n\n"
                f"**Error**: {str(e)}\n"
                f"**Issue**: #{issue_number}\n"
                f"**Action**: {action}\n\n"
                f"_Please check the workflow logs for details._"
            )

            self.add_issue_comment(issue_number, error_comment)
            raise


def main():
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(description="Update GitHub Project status based on issue assignment")
    parser.add_argument("--issue-number", type=int, required=True, help="Issue number")
    parser.add_argument("--action", choices=["assigned", "unassigned"], required=True, help="Assignment action")
    parser.add_argument("--assignees", nargs="*", default=[], help="List of assignee logins")
    parser.add_argument("--owner", help="Repository owner (default: from GITHUB_REPOSITORY)")
    parser.add_argument("--repo", help="Repository name (default: from GITHUB_REPOSITORY)")
    parser.add_argument("--token", help="GitHub token (default: from GITHUB_TOKEN)")

    args = parser.parse_args()

    # Get repository info from environment if not provided
    if not args.owner or not args.repo:
        github_repo = os.getenv("GITHUB_REPOSITORY", "")
        if "/" in github_repo:
            env_owner, env_repo = github_repo.split("/", 1)
            args.owner = args.owner or env_owner
            args.repo = args.repo or env_repo
        else:
            print("❌ Could not determine repository owner/name")
            sys.exit(1)

    # Get token from environment if not provided
    token = args.token or os.getenv("GITHUB_TOKEN")
    if not token:
        print("❌ GitHub token not provided")
        sys.exit(1)

    print(f"🚀 Starting project status update for {args.owner}/{args.repo}")
    print(f"📋 Issue: #{args.issue_number}")
    print(f"🔄 Action: {args.action}")
    print(f"👥 Assignees: {args.assignees}")

    try:
        updater = GitHubProjectUpdater(token, args.owner, args.repo)
        updater.update_issue_status(args.issue_number, args.action, args.assignees)
        print("✅ Project status update completed successfully")

    except Exception as e:
        print(f"❌ Script failed: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
