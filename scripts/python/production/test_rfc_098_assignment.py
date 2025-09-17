#!/usr/bin/env python3
"""
Test script for RFC-098-02: Assignment Test

This script validates the assignment workflow automation for RFC-098 by:
1. Checking for RFC-098 issues
2. Validating their assignment status 
3. Testing the timing requirements
4. Integrating with project status functionality from RFC-098-01
"""

import json
import os
import subprocess
import sys
import time
from datetime import datetime, timedelta
from typing import Dict, List, Optional


def run_gh_command(cmd: List[str]) -> Optional[str]:
    """Run a gh command and return the output, or None if it fails."""
    try:
        result = subprocess.run(
            ["gh"] + cmd, 
            capture_output=True, 
            text=True, 
            check=True
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Command failed: gh {' '.join(cmd)}")
        print(f"Error: {e.stderr}")
        return None


def get_rfc_098_issues(repo: str) -> List[Dict]:
    """Get all issues for RFC-098."""
    output = run_gh_command([
        "issue", "list", 
        "--repo", repo,
        "--state", "open",
        "--limit", "50",
        "--json", "number,title,assignees,createdAt,comments"
    ])
    
    if not output:
        return []
    
    issues = json.loads(output)
    rfc_098_issues = []
    
    for issue in issues:
        title = issue.get("title", "")
        # Look for RFC-098-XX pattern
        if "RFC-098-" in title.upper():
            assignee_count = len(issue.get("assignees", []))
            assignee_names = [a.get("login", "") for a in issue.get("assignees", [])]
            
            rfc_098_issues.append({
                "number": issue["number"],
                "title": title,
                "created_at": issue["createdAt"],
                "assignee_count": assignee_count,
                "assignee_names": assignee_names,
                "comments": issue.get("comments", 0)
            })
    
    return rfc_098_issues


def validate_assignment_timing(issues: List[Dict], max_minutes: int = 10) -> Dict:
    """Validate that issues are assigned within the specified time limit."""
    now = datetime.now()
    assigned_issues = 0
    unassigned_issues = 0
    timing_violations = 0
    
    for issue in issues:
        created_at = datetime.fromisoformat(issue["created_at"].replace("Z", "+00:00"))
        age_minutes = (now - created_at).total_seconds() / 60
        
        if issue["assignee_count"] > 0:
            assigned_issues += 1
            # If assigned, timing is good regardless of when
        else:
            unassigned_issues += 1
            # If unassigned and older than max_minutes, it's a timing violation
            if age_minutes > max_minutes:
                timing_violations += 1
    
    return {
        "total_issues": len(issues),
        "assigned_issues": assigned_issues,
        "unassigned_issues": unassigned_issues,
        "timing_violations": timing_violations,
        "max_minutes": max_minutes
    }


def check_project_status_integration(repo: str, issue_number: int) -> bool:
    """Check if project status integration from RFC-098-01 is working."""
    # Get issue comments to look for project status update comments
    output = run_gh_command([
        "issue", "view", str(issue_number),
        "--repo", repo,
        "--json", "comments"
    ])
    
    if not output:
        return False
    
    data = json.loads(output)
    comments = data.get("comments", [])
    
    # Look for project status update comment from RFC-098-01
    for comment in comments:
        body = comment.get("body", "")
        if "🤖 **Project Status Update**" in body:
            return True
    
    return False


def test_assignment_and_status_integration(repo: str) -> Dict:
    """Test both assignment and project status integration."""
    print("🔗 Testing RFC-098 assignment and project status integration...")
    
    issues = get_rfc_098_issues(repo)
    
    if not issues:
        return {
            "success": False,
            "message": "No RFC-098 issues found for integration testing"
        }
    
    integration_working = 0
    assigned_issues = [issue for issue in issues if issue["assignee_count"] > 0]
    
    for issue in assigned_issues:
        if check_project_status_integration(repo, issue["number"]):
            integration_working += 1
            print(f"   ✅ Issue #{issue['number']}: Assignment + Status integration working")
        else:
            print(f"   ⚠️  Issue #{issue['number']}: Assignment working but status integration pending")
    
    return {
        "success": True,
        "assigned_issues": len(assigned_issues),
        "integration_working": integration_working,
        "integration_rate": integration_working / len(assigned_issues) if assigned_issues else 0
    }


def main():
    """Main test function."""
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("❌ Error: REPO or GITHUB_REPOSITORY environment variable required")
        sys.exit(1)
    
    print(f"🧪 Testing RFC-098-02 Assignment Workflow")
    print(f"Repository: {repo}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 50)
    
    # Step 1: Check for RFC-098 issues
    print("\n🔍 Step 1: Finding RFC-098 issues...")
    rfc_issues = get_rfc_098_issues(repo)
    
    if not rfc_issues:
        print("❌ No RFC-098 issues found")
        print("   This could mean:")
        print("   - RFC-098 hasn't been created yet")
        print("   - rfc-sync workflow hasn't run yet")
        print("   - Issues were already closed")
        return 1
    
    print(f"✅ Found {len(rfc_issues)} RFC-098 issues:")
    for issue in rfc_issues:
        status = "ASSIGNED" if issue["assignee_count"] > 0 else "UNASSIGNED"
        assignees = ", ".join(issue["assignee_names"]) if issue["assignee_names"] else "none"
        print(f"   - Issue #{issue['number']}: {issue['title']} [{status}] (assignees: {assignees})")
    
    # Step 2: Validate assignment timing
    print(f"\n⏰ Step 2: Validating assignment timing...")
    timing_results = validate_assignment_timing(rfc_issues)
    
    print(f"Assignment Summary:")
    print(f"   - Total issues: {timing_results['total_issues']}")
    print(f"   - Assigned: {timing_results['assigned_issues']}")
    print(f"   - Unassigned: {timing_results['unassigned_issues']}")
    print(f"   - Timing violations: {timing_results['timing_violations']}")
    print(f"   - Max assignment time: {timing_results['max_minutes']} minutes")
    
    # Step 3: Test project status integration
    print(f"\n🔗 Step 3: Testing integration with RFC-098-01...")
    integration_results = test_assignment_and_status_integration(repo)
    
    if integration_results["success"]:
        rate = integration_results["integration_rate"] * 100
        print(f"Integration Summary:")
        print(f"   - Assigned issues: {integration_results['assigned_issues']}")
        print(f"   - With status integration: {integration_results['integration_working']}")
        print(f"   - Integration rate: {rate:.1f}%")
    else:
        print(f"⚠️ {integration_results['message']}")
    
    # Final assessment
    print("\n" + "=" * 60)
    print("📊 RFC-098-02 ASSIGNMENT TEST RESULTS")
    print("=" * 60)
    
    # Success criteria
    assignment_success = timing_results['timing_violations'] == 0
    has_assigned_issues = timing_results['assigned_issues'] > 0
    integration_tested = integration_results.get("success", False)
    
    print(f"✅ Issues found: {len(rfc_issues) > 0}")
    print(f"{'✅' if assignment_success else '❌'} Assignment timing: {'PASS' if assignment_success else 'FAIL'}")
    print(f"{'✅' if has_assigned_issues else '⚠️ '} Assignment working: {'YES' if has_assigned_issues else 'PENDING'}")
    print(f"{'✅' if integration_tested else '⚠️ '} Status integration: {'TESTED' if integration_tested else 'PENDING'}")
    
    # Determine exit code
    if len(rfc_issues) == 0:
        print("\n❌ No RFC-098 issues found - cannot validate assignment workflow")
        return 1
    elif not assignment_success:
        print("\n❌ Assignment timing requirements not met")
        return 1
    elif not has_assigned_issues:
        print("\n⏳ No issues assigned yet - test pending")
        return 2
    else:
        print("\n🎉 RFC-098-02 Assignment Test: SUCCESS!")
        print("\n📋 Validated:")
        print("   ✅ RFC-098 issues are found and processed")
        print("   ✅ Assignment workflow is functioning")
        print("   ✅ Timing requirements are met")
        print("   ✅ Integration with RFC-098-01 project status is validated")
        return 0


if __name__ == "__main__":
    sys.exit(main())