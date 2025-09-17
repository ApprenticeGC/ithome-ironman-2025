#!/usr/bin/env python3
"""
RFC-102-01: Final Project Board Integration Test

This script creates a test issue for RFC-102-01 and validates that the
update-project-board.yml workflow correctly adds project tracking comments
to RFC issues.
"""

import json
import os
import subprocess
import sys
import time
from datetime import datetime
from typing import Dict, List, Optional


def run_gh_command(args: List[str]) -> Optional[str]:
    """Run a GitHub CLI command and return the output."""
    try:
        result = subprocess.run(
            ["gh"] + args,
            capture_output=True,
            text=True,
            check=True
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"❌ GitHub CLI error: {e}")
        print(f"   Command: gh {' '.join(args)}")
        print(f"   Error output: {e.stderr}")
        return None


def create_test_issue(repo: str) -> Optional[int]:
    """Create the RFC-102-01 test issue."""
    title = "RFC-102-01: Final Project Board Integration Test"
    body = """## Summary

This is a test issue to validate the project board integration workflow.

## Acceptance Criteria

- [ ] Verify `update-project-board.yml` workflow triggers when this issue is created
- [ ] Confirm workflow correctly identifies this as an RFC issue
- [ ] Validate that project tracking comment is added automatically
- [ ] Verify comment contains correct project board link and status information
- [ ] Ensure comment format matches expected template

## Expected Workflow Behavior

When this issue is created, the `update-project-board.yml` workflow should:

1. Detect this as an RFC issue (title contains "RFC-")
2. Add a project tracking comment with:
   - Link to project board: https://github.com/users/ApprenticeGC/projects/2/views/1
   - Status: Added to RFC Backlog
   - Next step: Waiting for Copilot implementation
   - Progress tracking information

## Test Validation

This issue serves as a test case for the repaired project board workflow integration.
"""

    print("🔨 Creating RFC-102-01 test issue...")
    output = run_gh_command([
        "issue", "create",
        "--repo", repo,
        "--title", title,
        "--body", body,
        "--label", "rfc-102"
    ])
    
    if not output:
        return None
    
    # Extract issue number from URL
    try:
        issue_url = output.strip()
        issue_number = int(issue_url.split('/')[-1])
        print(f"✅ Created test issue #{issue_number}: {issue_url}")
        return issue_number
    except (ValueError, IndexError):
        print(f"❌ Could not parse issue number from: {output}")
        return None


def wait_for_workflow_completion(repo: str, issue_number: int, timeout_minutes: int = 5) -> bool:
    """Wait for the update-project-board.yml workflow to complete."""
    print(f"⏳ Waiting up to {timeout_minutes} minutes for workflow completion...")
    
    start_time = time.time()
    timeout_seconds = timeout_minutes * 60
    
    while time.time() - start_time < timeout_seconds:
        print("   Checking workflow status...")
        time.sleep(10)  # Check every 10 seconds
        
        # Check if workflow has run by looking for the expected comment
        comments = get_issue_comments(repo, issue_number)
        if comments and any("Project Tracking" in comment.get("body", "") for comment in comments):
            print("✅ Workflow appears to have completed (project tracking comment found)")
            return True
    
    print(f"⏰ Timeout after {timeout_minutes} minutes")
    return False


def get_issue_comments(repo: str, issue_number: int) -> Optional[List[Dict]]:
    """Get all comments for an issue."""
    output = run_gh_command([
        "issue", "view", str(issue_number),
        "--repo", repo,
        "--comments",
        "--json", "comments"
    ])
    
    if not output:
        return None
    
    try:
        data = json.loads(output)
        return data.get("comments", [])
    except json.JSONDecodeError as e:
        print(f"❌ Error parsing issue comments: {e}")
        return None


def validate_project_tracking_comment(comments: List[Dict]) -> bool:
    """Validate that the project tracking comment was added correctly."""
    print("🔍 Validating project tracking comment...")
    
    expected_markers = [
        "🎯 **Project Tracking**",
        "Project Board](https://github.com/users/ApprenticeGC/projects/2/views/1)",
        "📊 **Status**: Added to RFC Backlog",
        "🤖 **Next**: Waiting for Copilot implementation",
        "📈 **Track Progress**",
        "Project Board Integration workflow"
    ]
    
    project_comments = []
    for comment in comments:
        body = comment.get("body", "")
        if "Project Tracking" in body:
            project_comments.append(comment)
    
    if not project_comments:
        print("❌ No project tracking comment found")
        return False
    
    if len(project_comments) > 1:
        print(f"⚠️  Multiple project tracking comments found ({len(project_comments)})")
    
    # Validate the most recent project tracking comment
    comment_body = project_comments[-1]["body"]
    print("✅ Project tracking comment found")
    
    missing_markers = []
    for marker in expected_markers:
        if marker not in comment_body:
            missing_markers.append(marker)
    
    if missing_markers:
        print("❌ Project tracking comment is missing expected content:")
        for marker in missing_markers:
            print(f"   - Missing: {marker}")
        print("\n📄 Actual comment content:")
        print(comment_body)
        return False
    
    print("✅ Project tracking comment contains all expected content")
    return True


def cleanup_test_issue(repo: str, issue_number: int) -> bool:
    """Close and clean up the test issue."""
    print(f"🧹 Cleaning up test issue #{issue_number}...")
    
    # Add a cleanup comment
    cleanup_comment = """✅ **Test Complete**: Project board integration test completed successfully.

This test issue can now be closed as it has served its purpose of validating the workflow integration.

*Automated cleanup by RFC-102-01 integration test script*"""
    
    comment_result = run_gh_command([
        "issue", "comment", str(issue_number),
        "--repo", repo,
        "--body", cleanup_comment
    ])
    
    if not comment_result:
        print("⚠️  Could not add cleanup comment")
    
    # Close the issue
    close_result = run_gh_command([
        "issue", "close", str(issue_number),
        "--repo", repo,
        "--reason", "completed"
    ])
    
    if close_result:
        print(f"✅ Test issue #{issue_number} closed successfully")
        return True
    else:
        print(f"❌ Failed to close test issue #{issue_number}")
        return False


def main():
    """Main test function."""
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("❌ Error: REPO or GITHUB_REPOSITORY environment variable required")
        sys.exit(1)
    
    print(f"🧪 RFC-102-01: Final Project Board Integration Test")
    print(f"Repository: {repo}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 60)
    
    # Step 1: Create test issue
    print("\n🔍 Step 1: Creating test issue...")
    issue_number = create_test_issue(repo)
    if not issue_number:
        print("❌ Failed to create test issue")
        sys.exit(1)
    
    # Step 2: Wait for workflow completion
    print(f"\n⏳ Step 2: Waiting for workflow completion...")
    if not wait_for_workflow_completion(repo, issue_number):
        print("❌ Workflow did not complete in expected time")
        print("   Manual verification may be needed")
    
    # Step 3: Validate results
    print(f"\n🔍 Step 3: Validating workflow results...")
    comments = get_issue_comments(repo, issue_number)
    if not comments:
        print("❌ Could not retrieve issue comments")
        sys.exit(1)
    
    validation_success = validate_project_tracking_comment(comments)
    
    # Step 4: Report results
    print(f"\n📊 Step 4: Test Results...")
    if validation_success:
        print("✅ Project board integration test PASSED")
        print("   - RFC issue detection: ✅")
        print("   - Workflow trigger: ✅")
        print("   - Comment addition: ✅")
        print("   - Comment content: ✅")
    else:
        print("❌ Project board integration test FAILED")
        print("   Check the workflow logs and issue comments for details")
    
    # Step 5: Cleanup (optional)
    cleanup_input = input(f"\n🧹 Clean up test issue #{issue_number}? (y/N): ").strip().lower()
    if cleanup_input in ['y', 'yes']:
        cleanup_test_issue(repo, issue_number)
    else:
        print(f"ℹ️  Test issue #{issue_number} left open for manual inspection")
    
    print(f"\n🎯 RFC-102-01 Integration Test Complete!")
    return 0 if validation_success else 1


if __name__ == "__main__":
    sys.exit(main())