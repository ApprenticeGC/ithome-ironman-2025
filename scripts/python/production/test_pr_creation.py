#!/usr/bin/env python3
"""
Test script for RFC-092-02: Validate PR Creation

This script helps validate the PR creation functionality by:
1. Checking for RFC-092 PRs created by Copilot
2. Validating PR titles contain RFC identifiers 
3. Testing that PR bodies contain "Closes #<issue-number>"
"""

import json
import os
import re
import subprocess
import sys
from datetime import datetime
from typing import Dict, List, Optional


def run_gh_command(cmd: List[str]) -> Optional[str]:
    """Run a gh command and return the output, or None if it fails."""
    try:
        env = os.environ.copy()
        # Use token from environment for authentication
        env["GH_TOKEN"] = os.environ.get("AUTO_APPROVE_TOKEN") or os.environ.get("GH_TOKEN", "")
        
        result = subprocess.run(
            ["gh"] + cmd, 
            capture_output=True, 
            text=True, 
            check=True,
            env=env
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Command failed: gh {' '.join(cmd)}")
        print(f"Error: {e.stderr}")
        return None


def get_rfc_prs(repo: str, rfc_number: str = "092") -> List[Dict]:
    """Get all PRs for a specific RFC number."""
    output = run_gh_command([
        "pr", "list", 
        "--repo", repo,
        "--state", "open",
        "--limit", "50",
        "--json", "number,title,author,body,headRefName,createdAt"
    ])
    
    if not output:
        return []
    
    prs = json.loads(output)
    rfc_prs = []
    
    for pr in prs:
        title = pr.get("title", "")
        if f"RFC-{rfc_number}-" in title.upper():
            rfc_prs.append(pr)
    
    return rfc_prs


def validate_pr_requirements(prs: List[Dict]) -> Dict:
    """Validate PR requirements according to RFC-092-02 acceptance criteria."""
    results = {
        "total_prs": len(prs),
        "copilot_authored": 0,
        "has_rfc_identifier": 0, 
        "has_closes_link": 0,
        "fully_compliant": 0,
        "pr_details": []
    }
    
    # Allowed Copilot authors based on existing scripts
    allowed_authors = {
        "Copilot",
        "app/copilot-swe-agent", 
        "github-actions[bot]",
        "github-actions",
        "app/github-actions",
    }
    
    for pr in prs:
        pr_detail = {
            "number": pr["number"],
            "title": pr["title"],
            "author": pr.get("author", {}).get("login", ""),
            "body": pr.get("body", ""),
            "created_at": pr.get("createdAt", ""),
            "is_copilot_authored": False,
            "has_rfc_identifier": False,
            "has_closes_link": False,
            "compliance_issues": []
        }
        
        # Check 1: PR created by copilot-swe-agent
        if pr_detail["author"] in allowed_authors:
            pr_detail["is_copilot_authored"] = True
            results["copilot_authored"] += 1
        else:
            pr_detail["compliance_issues"].append(f"Author '{pr_detail['author']}' is not a recognized Copilot agent")
        
        # Check 2: PR title contains RFC identifier
        if re.search(r"RFC-\d{3}-\d{2}", pr_detail["title"], re.IGNORECASE):
            pr_detail["has_rfc_identifier"] = True
            results["has_rfc_identifier"] += 1
        else:
            pr_detail["compliance_issues"].append("Title does not contain RFC identifier (RFC-XXX-XX)")
        
        # Check 3: PR body contains "Closes #<issue-number>"
        if re.search(r"\b(close[sd]?|fixe?[sd]?|resolve[sd]?) #[0-9]+", pr_detail["body"], re.IGNORECASE):
            pr_detail["has_closes_link"] = True
            results["has_closes_link"] += 1
        else:
            pr_detail["compliance_issues"].append("Body does not contain 'Closes #<issue-number>' link")
        
        # Check if fully compliant
        if (pr_detail["is_copilot_authored"] and 
            pr_detail["has_rfc_identifier"] and 
            pr_detail["has_closes_link"]):
            results["fully_compliant"] += 1
        
        results["pr_details"].append(pr_detail)
    
    return results


def main():
    """Main test function."""
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("❌ Error: REPO or GITHUB_REPOSITORY environment variable required")
        sys.exit(1)
    
    print(f"🧪 Testing RFC-092-02 PR Creation Flow")
    print(f"Repository: {repo}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("=" * 50)
    
    # Step 1: Check for RFC-092 PRs
    print("\n🔍 Step 1: Finding RFC-092 PRs...")
    rfc_prs = get_rfc_prs(repo, "092")
    
    if not rfc_prs:
        print("❌ No RFC-092 PRs found")
        print("   This could mean:")
        print("   - RFC-092 issues haven't been assigned to Copilot yet")
        print("   - Copilot hasn't created PRs for assigned issues yet")
        print("   - PRs were already merged/closed")
        return 1
    
    print(f"✅ Found {len(rfc_prs)} RFC-092 PRs:")
    for pr in rfc_prs:
        author = pr.get("author", {}).get("login", "unknown")
        print(f"   - PR #{pr['number']}: {pr['title']} (author: {author})")
    
    # Step 2: Validate PR requirements
    print(f"\n🔍 Step 2: Validating PR requirements...")
    validation_results = validate_pr_requirements(rfc_prs)
    
    print(f"Validation Summary:")
    print(f"   - Total PRs: {validation_results['total_prs']}")
    print(f"   - Copilot authored: {validation_results['copilot_authored']}")
    print(f"   - Has RFC identifier: {validation_results['has_rfc_identifier']}")
    print(f"   - Has closes link: {validation_results['has_closes_link']}")
    print(f"   - Fully compliant: {validation_results['fully_compliant']}")
    
    # Step 3: Detailed validation results
    print(f"\n🔍 Step 3: Detailed validation results...")
    for pr_detail in validation_results["pr_details"]:
        print(f"\nPR #{pr_detail['number']}: {pr_detail['title']}")
        print(f"   Author: {pr_detail['author']}")
        if pr_detail["compliance_issues"]:
            print("   Issues:")
            for issue in pr_detail["compliance_issues"]:
                print(f"     ❌ {issue}")
        else:
            print("   ✅ All requirements met")
    
    # Step 4: Final validation
    print(f"\n🎯 Step 4: Final validation...")
    
    success = True
    
    # Check acceptance criteria
    print("\nAcceptance Criteria Validation:")
    
    # Criterion 1: PR created automatically by copilot-swe-agent
    if validation_results["copilot_authored"] > 0:
        print("   ✅ PR created automatically by copilot-swe-agent")
    else:
        print("   ❌ PR created automatically by copilot-swe-agent")
        success = False
    
    # Criterion 2: PR title contains RFC identifier
    if validation_results["has_rfc_identifier"] > 0:
        print("   ✅ PR title contains RFC identifier")
    else:
        print("   ❌ PR title contains RFC identifier")
        success = False
    
    # Criterion 3: PR body contains "Closes #<issue-number>"
    if validation_results["has_closes_link"] > 0:
        print("   ✅ PR body contains \"Closes #<issue-number>\"")
    else:
        print("   ❌ PR body contains \"Closes #<issue-number>\"")
        success = False
    
    print("\n" + "=" * 50)
    if success and validation_results["fully_compliant"] > 0:
        print("🎉 RFC-092-02 Test: PASSED")
        print("All acceptance criteria have been met!")
        return 0
    elif validation_results["total_prs"] == 0:
        print("⏳ RFC-092-02 Test: PENDING")
        print("No RFC-092 PRs found yet...")
        print("Copilot may not have been assigned issues yet, or hasn't created PRs")
        return 2
    else:
        print("❌ RFC-092-02 Test: FAILED")
        print("Some acceptance criteria were not met")
        return 1


if __name__ == "__main__":
    sys.exit(main())