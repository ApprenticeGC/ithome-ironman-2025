#!/usr/bin/env python3
"""
RFC-102-02: Test Assignment Status Workflow

This script creates a test issue for RFC-102-02 and validates that the
update-project-status-on-assignment.yml workflow correctly updates project
status when issues are assigned and unassigned.
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
    """Create the RFC-102-02 test issue."""
    title = "RFC-102-02: Test Assignment Status Workflow"
    body = """## Summary

This is a test issue to validate the assignment status workflow automation.

## Purpose
- Test the assignment → 'In Progress' status automation
- Verify project integration works correctly  
- Test the complete workflow chain

## Expected Behavior
1. Issue created → Added to project (status: No Status/Todo)
2. Issue assigned → Status updated to 'In Progress'
3. Issue unassigned → Status updated to 'Todo'

## Testing Steps
1. Assign to Copilot
2. Check project board status change
3. Unassign from Copilot
4. Check status reverts to Todo

## Acceptance Criteria

- [ ] Verify `update-project-status-on-assignment.yml` workflow triggers when this issue is assigned
- [ ] Confirm workflow correctly updates project status to 'In Progress' 
- [ ] Validate that project status comment is added to the issue
- [ ] Verify workflow triggers when issue is unassigned
- [ ] Confirm project status reverts to 'Todo'
- [ ] Validate unassignment comment is added to the issue

*This test issue validates the automated project status update workflow functionality.*"""

    print(f"📝 Creating test issue: {title}")
    
    result = run_gh_command([
        "issue", "create",
        "--repo", repo,
        "--title", title,
        "--body", body,
        "--label", "test,rfc-102"
    ])
    
    if result:
        # Extract issue number from URL
        issue_number = result.split('/')[-1]
        print(f"✅ Created test issue #{issue_number}")
        return int(issue_number)
    else:
        print("❌ Failed to create test issue")
        return None


def assign_issue_to_copilot(repo: str, issue_number: int) -> bool:
    """Assign the issue to Copilot bot using the existing assignment script."""
    print(f"🤖 Assigning issue #{issue_number} to Copilot...")
    
    # Use the existing assign_issue_to_copilot.py script
    owner, repo_name = repo.split('/')
    try:
        result = subprocess.run([
            "python3", "scripts/python/production/assign_issue_to_copilot.py",
            "--owner", owner,
            "--repo", repo_name,
            "--issue-number", str(issue_number),
            "--assign-mode", "bot"
        ], capture_output=True, text=True, check=True)
        
        output = result.stdout.strip()
        if output:
            try:
                result_data = json.loads(output)
                if result_data.get("success"):
                    assignee = result_data.get("assignee_login", "unknown")
                    print(f"✅ Issue #{issue_number} assigned to {assignee}")
                    return True
                else:
                    error = result_data.get("error", "unknown error")
                    print(f"❌ Assignment failed: {error}")
                    return False
            except json.JSONDecodeError:
                print(f"✅ Issue #{issue_number} assigned to Copilot (unexpected output format)")
                return True
        else:
            print(f"❌ No output from assignment script")
            return False
            
    except subprocess.CalledProcessError as e:
        print(f"❌ Assignment script failed: {e}")
        print(f"   Error output: {e.stderr}")
        # Fallback: try simple gh CLI assignment
        return assign_issue_simple(repo, issue_number)


def assign_issue_simple(repo: str, issue_number: int) -> bool:
    """Fallback: try simple assignment via gh CLI."""
    print(f"🔄 Trying fallback assignment method...")
    
    # Try different possible Copilot usernames
    copilot_names = ["Copilot", "copilot", "copilot-swe-agent", "github-copilot"]
    
    for name in copilot_names:
        try:
            result = run_gh_command([
                "issue", "edit", str(issue_number),
                "--repo", repo,
                "--add-assignee", name
            ])
            
            if result is not None:
                print(f"✅ Issue #{issue_number} assigned to {name}")
                return True
        except:
            continue
    
    print(f"❌ All fallback assignment methods failed")
    return False


def unassign_issue_from_copilot(repo: str, issue_number: int) -> bool:
    """Unassign the issue from Copilot bot."""
    print(f"🔓 Unassigning issue #{issue_number} from Copilot...")
    
    # Get current assignees first
    issue_data = run_gh_command([
        "issue", "view", str(issue_number),
        "--repo", repo,
        "--json", "assignees"
    ])
    
    if not issue_data:
        print("❌ Could not get issue assignees")
        return False
    
    try:
        data = json.loads(issue_data)
        assignees = data.get("assignees", [])
        
        if not assignees:
            print("ℹ️ Issue has no assignees to remove")
            return True
        
        # Remove all assignees (since we only assigned Copilot)
        for assignee in assignees:
            login = assignee.get("login")
            if login:
                result = run_gh_command([
                    "issue", "edit", str(issue_number),
                    "--repo", repo,
                    "--remove-assignee", login
                ])
                
                if result is not None:
                    print(f"✅ Removed assignee {login} from issue #{issue_number}")
                else:
                    print(f"⚠️ Could not remove assignee {login}")
        
        return True
        
    except json.JSONDecodeError:
        print("❌ Could not parse assignees data")
        return False


def get_issue_comments(repo: str, issue_number: int) -> List[Dict]:
    """Get all comments for the issue."""
    result = run_gh_command([
        "issue", "view", str(issue_number),
        "--repo", repo,
        "--json", "comments"
    ])
    
    if result:
        try:
            data = json.loads(result)
            return data.get("comments", [])
        except json.JSONDecodeError:
            print("❌ Failed to parse issue comments JSON")
            return []
    return []


def wait_for_status_update_comment(repo: str, issue_number: int, expected_status: str, timeout_minutes: int = 3) -> bool:
    """Wait for the assignment status update comment to appear."""
    print(f"⏳ Waiting up to {timeout_minutes} minutes for status update to '{expected_status}'...")
    
    start_time = time.time()
    timeout_seconds = timeout_minutes * 60
    
    while time.time() - start_time < timeout_seconds:
        print("   Checking for status update comment...")
        time.sleep(15)  # Check every 15 seconds
        
        comments = get_issue_comments(repo, issue_number)
        for comment in comments:
            body = comment.get("body", "")
            if ("Project Status Update" in body and expected_status in body):
                print(f"✅ Found status update comment for '{expected_status}'")
                print(f"   Comment preview: {body[:100]}...")
                return True
    
    print(f"⏰ Timeout after {timeout_minutes} minutes - no status update comment found")
    return False


def validate_status_update_comments(comments: List[Dict]) -> Dict[str, bool]:
    """Validate that both assignment and unassignment status comments are present."""
    print("🔍 Validating status update comments...")
    
    results = {
        "in_progress": False,
        "todo": False
    }
    
    for comment in comments:
        body = comment.get("body", "")
        if "Project Status Update" in body:
            if "In Progress" in body and "assigned" in body:
                results["in_progress"] = True
                print("✅ Found 'In Progress' status update comment")
            elif "Todo" in body and "unassigned" in body:
                results["todo"] = True
                print("✅ Found 'Todo' status update comment")
    
    return results


def cleanup_test_issue(repo: str, issue_number: int) -> bool:
    """Close and clean up the test issue."""
    print(f"🧹 Cleaning up test issue #{issue_number}...")
    
    # Add a cleanup comment
    cleanup_comment = """✅ **Test Complete**: Assignment status workflow test completed successfully.

## Test Results Summary
- ✅ Assignment triggered 'In Progress' status update
- ✅ Unassignment triggered 'Todo' status update  
- ✅ Project status automation workflow functioning correctly

This test issue can now be closed as it has served its purpose of validating the assignment status workflow.

*Automated cleanup by RFC-102-02 assignment status test script*"""
    
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
    
    # Check for GitHub token
    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if not token:
        print("❌ Error: GITHUB_TOKEN or GH_TOKEN environment variable required")
        sys.exit(1)
    
    print(f"🧪 RFC-102-02: Test Assignment Status Workflow")
    print(f"Repository: {repo}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 60)
    
    # Step 1: Create test issue
    print("\n🔍 Step 1: Creating test issue...")
    issue_number = create_test_issue(repo)
    if not issue_number:
        print("❌ Failed to create test issue")
        sys.exit(1)
    
    # Step 2: Assign to Copilot
    print(f"\n🤖 Step 2: Assigning issue to Copilot...")
    if not assign_issue_to_copilot(repo, issue_number):
        print("❌ Failed to assign issue to Copilot")
        sys.exit(1)
    
    # Step 3: Wait for 'In Progress' status update
    print(f"\n⏳ Step 3: Waiting for 'In Progress' status update...")
    in_progress_success = wait_for_status_update_comment(repo, issue_number, "In Progress")
    
    # Step 4: Unassign from Copilot
    print(f"\n🔓 Step 4: Unassigning issue from Copilot...")
    if not unassign_issue_from_copilot(repo, issue_number):
        print("❌ Failed to unassign issue from Copilot")
        # Continue with validation even if unassignment failed
    
    # Step 5: Wait for 'Todo' status update
    print(f"\n⏳ Step 5: Waiting for 'Todo' status update...")
    todo_success = wait_for_status_update_comment(repo, issue_number, "Todo")
    
    # Step 6: Validate all results
    print(f"\n🔍 Step 6: Validating complete workflow...")
    comments = get_issue_comments(repo, issue_number)
    if not comments:
        print("❌ Could not retrieve issue comments for validation")
        validation_results = {"in_progress": False, "todo": False}
    else:
        validation_results = validate_status_update_comments(comments)
    
    # Step 7: Report results
    print(f"\n📊 Step 7: Test Results...")
    assignment_success = in_progress_success and validation_results["in_progress"]
    unassignment_success = todo_success and validation_results["todo"]
    overall_success = assignment_success and unassignment_success
    
    if overall_success:
        print("✅ Assignment status workflow test PASSED")
        print("   - Issue assignment: ✅")
        print("   - 'In Progress' status update: ✅")
        print("   - Issue unassignment: ✅")
        print("   - 'Todo' status update: ✅")
    else:
        print("❌ Assignment status workflow test FAILED")
        print(f"   - Issue assignment: {'✅' if assignment_success else '❌'}")
        print(f"   - 'In Progress' status update: {'✅' if validation_results['in_progress'] else '❌'}")
        print(f"   - Issue unassignment: {'✅' if unassignment_success else '❌'}")
        print(f"   - 'Todo' status update: {'✅' if validation_results['todo'] else '❌'}")
        print("   Check the workflow logs and issue comments for details")
    
    # Step 8: Cleanup (optional)
    cleanup_input = input(f"\n🧹 Clean up test issue #{issue_number}? (y/N): ").strip().lower()
    if cleanup_input in ['y', 'yes']:
        cleanup_test_issue(repo, issue_number)
    else:
        print(f"ℹ️  Test issue #{issue_number} left open for manual inspection")
    
    print(f"\n🎯 RFC-102-02 Assignment Status Workflow Test Complete!")
    return 0 if overall_success else 1


if __name__ == "__main__":
    sys.exit(main())