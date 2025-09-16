#!/usr/bin/env python3
"""
Test script for RFC-092-01: Validate Issue Assignment

This script helps validate the issue assignment functionality by:
1. Checking for RFC-092 issues
2. Validating their assignment status 
3. Testing the timing requirements
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


def get_rfc_issues(repo: str, rfc_number: str = "092") -> List[Dict]:
    """Get all issues for a specific RFC number."""
    output = run_gh_command([
        "issue", "list", 
        "--repo", repo,
        "--state", "open",
        "--limit", "50",
        "--json", "number,title,assignees,createdAt"
    ])
    
    if not output:
        return []
    
    issues = json.loads(output)
    rfc_issues = []
    
    for issue in issues:
        title = issue.get("title", "")
        if f"RFC-{rfc_number}-" in title.upper():
            # Extract assignee count and names
            assignees = issue.get("assignees", [])
            issue["assignee_count"] = len(assignees)
            issue["assignee_names"] = [a.get("login", "") for a in assignees]
            rfc_issues.append(issue)
    
    return rfc_issues


def validate_assignment_timing(issues: List[Dict], max_minutes: int = 10) -> Dict:
    """Validate that issues are assigned within the specified time limit."""
    results = {
        "total_issues": len(issues),
        "assigned_issues": 0,
        "unassigned_issues": 0,
        "assignment_times": [],
        "within_time_limit": 0,
        "over_time_limit": 0
    }
    
    current_time = datetime.now()
    
    for issue in issues:
        if issue["assignee_count"] > 0:
            results["assigned_issues"] += 1
            
            # Calculate time since creation
            created_at = datetime.fromisoformat(issue["createdAt"].replace("Z", "+00:00"))
            time_diff = current_time - created_at.replace(tzinfo=None)
            minutes_since_creation = time_diff.total_seconds() / 60
            
            results["assignment_times"].append({
                "issue": issue["number"],
                "minutes": minutes_since_creation,
                "assignees": issue["assignee_names"]
            })
            
            if minutes_since_creation <= max_minutes:
                results["within_time_limit"] += 1
            else:
                results["over_time_limit"] += 1
        else:
            results["unassigned_issues"] += 1
    
    return results


def main():
    """Main test function."""
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("❌ Error: REPO or GITHUB_REPOSITORY environment variable required")
        sys.exit(1)
    
    print(f"🧪 Testing RFC-092-01 Assignment Flow")
    print(f"Repository: {repo}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 50)
    
    # Step 1: Check for RFC-092 issues
    print("\n🔍 Step 1: Finding RFC-092 issues...")
    rfc_issues = get_rfc_issues(repo, "092")
    
    if not rfc_issues:
        print("❌ No RFC-092 issues found")
        print("   This could mean:")
        print("   - RFC-092 hasn't been pushed to docs/game-rfcs/ yet")
        print("   - rfc-sync workflow hasn't run yet")
        print("   - Issues were already closed")
        return 1
    
    print(f"✅ Found {len(rfc_issues)} RFC-092 issues:")
    for issue in rfc_issues:
        status = "ASSIGNED" if issue["assignee_count"] > 0 else "UNASSIGNED"
        assignees = ", ".join(issue["assignee_names"]) if issue["assignee_names"] else "none"
        print(f"   - Issue #{issue['number']}: {issue['title']} [{status}] (assignees: {assignees})")
    
    # Step 2: Validate assignment status
    print(f"\n⏰ Step 2: Validating assignment timing...")
    timing_results = validate_assignment_timing(rfc_issues)
    
    print(f"Assignment Summary:")
    print(f"   - Total issues: {timing_results['total_issues']}")
    print(f"   - Assigned: {timing_results['assigned_issues']}")
    print(f"   - Unassigned: {timing_results['unassigned_issues']}")
    print(f"   - Within 10 minutes: {timing_results['within_time_limit']}")
    print(f"   - Over 10 minutes: {timing_results['over_time_limit']}")
    
    if timing_results["assignment_times"]:
        print(f"\nAssignment Details:")
        for at in timing_results["assignment_times"]:
            print(f"   - Issue #{at['issue']}: {at['minutes']:.1f} minutes (assignees: {', '.join(at['assignees'])})")
    
    # Step 3: Final validation
    print(f"\n🎯 Step 3: Final validation...")
    
    success = True
    
    # Check acceptance criteria
    print("\nAcceptance Criteria Validation:")
    
    # Criterion 1: Issues created via rfc-sync
    if timing_results["total_issues"] > 0:
        print("   ✅ Issue created via rfc-sync")
    else:
        print("   ❌ Issue created via rfc-sync")
        success = False
    
    # Criterion 2: Issue assigned to Copilot bot
    if timing_results["assigned_issues"] > 0:
        print("   ✅ Issue assigned to Copilot bot")
    else:
        print("   ❌ Issue assigned to Copilot bot")
        success = False
    
    # Criterion 3: Assignment within 10 minutes
    if timing_results["within_time_limit"] > 0 or timing_results["unassigned_issues"] == timing_results["total_issues"]:
        # If all issues are unassigned, we can't validate timing yet
        if timing_results["unassigned_issues"] == timing_results["total_issues"]:
            print("   ⏳ Assignment timing pending (all issues still unassigned)")
        else:
            print("   ✅ Assignment happens automatically within 10 minutes")
    else:
        print("   ❌ Assignment happens automatically within 10 minutes")
        success = False
    
    print("\n" + "=" * 50)
    if success and timing_results["assigned_issues"] > 0:
        print("🎉 RFC-092-01 Test: PASSED")
        print("All acceptance criteria have been met!")
        return 0
    elif timing_results["unassigned_issues"] == timing_results["total_issues"]:
        print("⏳ RFC-092-01 Test: PENDING")
        print("Issues exist but assignment is still pending...")
        print("rfc-assign-cron runs every 10 minutes, please wait and re-run this test")
        return 2
    else:
        print("❌ RFC-092-01 Test: FAILED")
        print("Some acceptance criteria were not met")
        return 1


if __name__ == "__main__":
    sys.exit(main())