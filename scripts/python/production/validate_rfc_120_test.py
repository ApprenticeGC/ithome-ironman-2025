#!/usr/bin/env python3
"""
RFC-120-01: Final Automation Test Validation Script

This script helps monitor and validate the automation test described in RFC-120-01.
It checks for the expected automation workflow results.
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


def get_issue_comments(repo: str, issue_number: int) -> List[Dict]:
    """Get comments for a specific issue."""
    output = run_gh_command([
        "issue", "view", str(issue_number),
        "--repo", repo,
        "--json", "comments"
    ])
    
    if not output:
        return []
    
    try:
        data = json.loads(output)
        return data.get("comments", [])
    except json.JSONDecodeError:
        print(f"❌ Failed to parse issue comments JSON")
        return []


def get_issue_info(repo: str, issue_number: int) -> Optional[Dict]:
    """Get information about a specific issue."""
    output = run_gh_command([
        "issue", "view", str(issue_number),
        "--repo", repo,
        "--json", "number,title,assignees,state,createdAt"
    ])
    
    if not output:
        return None
    
    try:
        return json.loads(output)
    except json.JSONDecodeError:
        print(f"❌ Failed to parse issue info JSON")
        return None


def validate_project_tracking_comment(comments: List[Dict]) -> bool:
    """Validate that the project tracking comment exists and has correct content."""
    for comment in comments:
        body = comment.get("body", "")
        if "🎯 **Project Tracking**" in body and "Project Board" in body:
            # Check for expected content
            expected_elements = [
                "https://github.com/users/ApprenticeGC/projects/2/views/1",
                "📊 **Status**: Added to RFC Backlog",
                "🤖 **Next**: Waiting for Copilot implementation",
                "📈 **Track Progress**",
                "Project Board Integration workflow"
            ]
            
            missing_elements = []
            for element in expected_elements:
                if element not in body:
                    missing_elements.append(element)
            
            if not missing_elements:
                return True
            else:
                print(f"⚠️ Project tracking comment found but missing elements:")
                for element in missing_elements:
                    print(f"   - {element}")
                return False
    
    return False


def validate_status_update_comment(comments: List[Dict]) -> bool:
    """Validate that the status update comment exists."""
    for comment in comments:
        body = comment.get("body", "")
        if "🤖 **Project Status Update**" in body and "In Progress" in body:
            return True
    
    return False


def main():
    """Main validation function."""
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("❌ Error: REPO or GITHUB_REPOSITORY environment variable required")
        sys.exit(1)
    
    # Look for RFC-120-01 issue - this should be provided as argument or detected
    issue_number = None
    if len(sys.argv) > 1:
        try:
            issue_number = int(sys.argv[1])
        except ValueError:
            print("❌ Error: Issue number must be an integer")
            sys.exit(1)
    else:
        print("ℹ️ No issue number provided. Please provide the RFC-120-01 issue number as an argument.")
        print("   Usage: python validate_rfc_120_test.py <issue_number>")
        sys.exit(1)
    
    print(f"🧪 Validating RFC-120-01 Final Automation Test")
    print(f"Repository: {repo}")
    print(f"Issue: #{issue_number}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 60)
    
    # Step 1: Get issue information
    print("\n🔍 Step 1: Getting issue information...")
    issue_info = get_issue_info(repo, issue_number)
    if not issue_info:
        print("❌ Failed to retrieve issue information")
        sys.exit(1)
    
    title = issue_info.get("title", "")
    assignees = issue_info.get("assignees", [])
    state = issue_info.get("state", "")
    
    print(f"   Title: {title}")
    print(f"   State: {state}")
    print(f"   Assignees: {[a.get('login') for a in assignees]}")
    
    # Verify this is RFC-120-01
    if "RFC-120-01" not in title:
        print("⚠️ Warning: Issue title doesn't contain 'RFC-120-01'")
    
    # Step 2: Check project tracking comment (Step 2 of the test)
    print("\n🔍 Step 2: Checking for project tracking comment...")
    comments = get_issue_comments(repo, issue_number)
    
    has_project_comment = validate_project_tracking_comment(comments)
    if has_project_comment:
        print("   ✅ Project tracking comment found and validated")
    else:
        print("   ❌ Project tracking comment missing or invalid")
    
    # Step 3: Check assignment status (Step 3 of the test)
    print("\n🔍 Step 3: Checking assignment status...")
    is_assigned = len(assignees) > 0
    copilot_assigned = any("copilot" in a.get("login", "").lower() for a in assignees)
    
    if is_assigned:
        assignee_names = [a.get('login') for a in assignees]
        print(f"   ✅ Issue is assigned to: {', '.join(assignee_names)}")
        if copilot_assigned:
            print("   ✅ Copilot is assigned")
        else:
            print("   ⚠️ Copilot not in assignee list")
    else:
        print("   ❌ Issue is not assigned")
    
    # Step 4: Check status update comment (Step 4 of the test)
    print("\n🔍 Step 4: Checking for status update comment...")
    has_status_comment = validate_status_update_comment(comments)
    if has_status_comment:
        print("   ✅ Status update comment found")
    else:
        print("   ❌ Status update comment missing")
    
    # Final assessment
    print("\n" + "=" * 60)
    test_steps = [
        ("Issue created", True),  # Always true since we're validating an existing issue
        ("Project tracking comment", has_project_comment),
        ("Manual assignment", is_assigned),
        ("Status update automation", has_status_comment and is_assigned)
    ]
    
    passed_steps = sum(1 for _, passed in test_steps if passed)
    total_steps = len(test_steps)
    
    print(f"🧪 RFC-120-01 Final Automation Test Results:")
    print(f"   Completed: {passed_steps}/{total_steps} steps")
    print()
    
    for step_name, passed in test_steps:
        status = "✅" if passed else "❌"
        print(f"   {status} {step_name}")
    
    print()
    if passed_steps == total_steps:
        print("🎉 RFC-120-01 Test: PASSED")
        print("   All automation workflow steps completed successfully!")
    else:
        print("⚠️ RFC-120-01 Test: INCOMPLETE")
        print("   Some automation steps are still pending or failed")
        if not has_project_comment:
            print("   💡 Tip: Project tracking comment should appear within 10 minutes of issue creation")
        if not is_assigned:
            print("   💡 Tip: Use the 'assign-copilot-to-issue.yml' workflow to test manual assignment")
        if is_assigned and not has_status_comment:
            print("   💡 Tip: Status update comment should appear within 5 minutes of assignment")
    
    return 0 if passed_steps == total_steps else 1


if __name__ == "__main__":
    sys.exit(main())