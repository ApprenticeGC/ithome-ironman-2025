#!/usr/bin/env python3
"""
RFC-099-01: Test Complete Diagnostic Workflow Automation

This script tests the complete end-to-end automation pipeline with
comprehensive diagnostic workflow analysis and blocker detection.
"""

import json
import os
import subprocess
import sys
import time
from datetime import datetime
from typing import Dict, List, Optional, Any


class DiagnosticTester:
    def __init__(self, repo: str):
        self.repo = repo
        self.test_results = {
            "timestamp": datetime.now().isoformat(),
            "repo": repo,
            "stages": {},
            "blockers": [],
            "overall_success": False
        }

    def run_command(self, cmd: List[str], check: bool = True) -> subprocess.CompletedProcess:
        """Run a command and return the result."""
        return subprocess.run(
            cmd, 
            check=check, 
            text=True, 
            capture_output=True,
            env={**os.environ, "GH_TOKEN": os.environ.get("AUTO_APPROVE_TOKEN") or os.environ.get("GH_TOKEN", "")}
        )

    def gh_json(self, cmd: List[str]) -> Any:
        """Run a gh command and return JSON output."""
        result = self.run_command(cmd)
        return json.loads(result.stdout)

    def log_diagnostic(self, stage: str, status: str, message: str, blocker_level: str = "info"):
        """Log diagnostic information for a stage."""
        print(f"🔍 [{stage}] {status}: {message}")
        
        self.test_results["stages"][stage] = {
            "status": status,
            "message": message,
            "timestamp": datetime.now().isoformat(),
            "blocker_level": blocker_level
        }
        
        if blocker_level in ["warning", "critical"]:
            self.test_results["blockers"].append({
                "stage": stage,
                "level": blocker_level,
                "message": message,
                "timestamp": datetime.now().isoformat()
            })

    def test_issue_assignment_diagnostics(self) -> bool:
        """Test that issues are properly assigned with diagnostic feedback."""
        self.log_diagnostic("issue_assignment", "starting", "Testing issue assignment diagnostics")
        
        try:
            # Look for RFC-099 issues
            issues = self.gh_json([
                "gh", "issue", "list", "--repo", self.repo,
                "--label", "rfc", "--state", "open",
                "--json", "number,title,assignees,createdAt"
            ])
            
            rfc_099_issues = [issue for issue in issues if "RFC-099" in issue.get("title", "")]
            
            if not rfc_099_issues:
                self.log_diagnostic("issue_assignment", "warning", 
                                  "No RFC-099 issues found - may need manual RFC sync", "warning")
                return False
                
            assigned_issues = [issue for issue in rfc_099_issues if issue.get("assignees")]
            
            if not assigned_issues:
                self.log_diagnostic("issue_assignment", "warning",
                                  "RFC-099 issues exist but none are assigned to Copilot", "warning")
                return False
                
            self.log_diagnostic("issue_assignment", "success", 
                              f"Found {len(assigned_issues)} assigned RFC-099 issues")
            return True
            
        except Exception as e:
            self.log_diagnostic("issue_assignment", "error", f"Failed to check issue assignment: {e}", "critical")
            return False

    def test_pr_creation_diagnostics(self) -> bool:
        """Test PR creation with diagnostic analysis."""
        self.log_diagnostic("pr_creation", "starting", "Testing PR creation diagnostics")
        
        try:
            # Look for RFC-099 PRs
            prs = self.gh_json([
                "gh", "pr", "list", "--repo", self.repo,
                "--state", "open", "--json", "number,title,author,body,createdAt"
            ])
            
            rfc_099_prs = [pr for pr in prs if "RFC-099" in pr.get("title", "")]
            
            if not rfc_099_prs:
                self.log_diagnostic("pr_creation", "warning",
                                  "No RFC-099 PRs found - Copilot may not have created PR yet", "warning")
                return False
            
            # Validate PR requirements
            compliant_prs = []
            for pr in rfc_099_prs:
                author = pr.get("author", {}).get("login", "")
                title = pr.get("title", "")
                body = pr.get("body", "")
                
                issues = []
                if author not in ["Copilot", "app/copilot-swe-agent", "github-actions[bot]"]:
                    issues.append(f"Non-Copilot author: {author}")
                if "RFC-099" not in title:
                    issues.append("Missing RFC-099 identifier in title")
                if "Closes #" not in body:
                    issues.append("Missing 'Closes #' link in body")
                
                if not issues:
                    compliant_prs.append(pr)
                else:
                    self.log_diagnostic("pr_creation", "warning",
                                      f"PR #{pr['number']} has issues: {', '.join(issues)}", "warning")
            
            if compliant_prs:
                self.log_diagnostic("pr_creation", "success", 
                                  f"Found {len(compliant_prs)} compliant RFC-099 PRs")
                return True
            else:
                self.log_diagnostic("pr_creation", "error",
                                  "No fully compliant RFC-099 PRs found", "critical")
                return False
                
        except Exception as e:
            self.log_diagnostic("pr_creation", "error", f"Failed to check PR creation: {e}", "critical")
            return False

    def test_ci_diagnostics(self) -> bool:
        """Test CI workflow with enhanced diagnostic validation."""
        self.log_diagnostic("ci_diagnostics", "starting", "Testing CI diagnostics")
        
        try:
            # Get workflow runs for RFC-099 PRs
            runs = self.gh_json([
                "gh", "run", "list", "--repo", self.repo,
                "--workflow", "ci", "--limit", "10",
                "--json", "conclusion,status,createdAt,headBranch,headSha"
            ])
            
            rfc_099_runs = [run for run in runs if "copilot/fix-99" in run.get("headBranch", "")]
            
            if not rfc_099_runs:
                self.log_diagnostic("ci_diagnostics", "info",
                                  "No CI runs found for RFC-099 branch yet")
                return True  # Not a failure, just not ready yet
            
            latest_run = rfc_099_runs[0]
            status = latest_run.get("status", "")
            conclusion = latest_run.get("conclusion", "")
            
            if status == "in_progress":
                self.log_diagnostic("ci_diagnostics", "info", "CI run in progress")
                return True
            elif conclusion == "success":
                self.log_diagnostic("ci_diagnostics", "success", "CI run completed successfully")
                return True
            elif conclusion == "failure":
                self.log_diagnostic("ci_diagnostics", "error", 
                                  "CI run failed - requires investigation", "critical")
                return False
            else:
                self.log_diagnostic("ci_diagnostics", "warning",
                                  f"CI run in unexpected state: {status}/{conclusion}", "warning")
                return False
                
        except Exception as e:
            self.log_diagnostic("ci_diagnostics", "error", f"Failed to check CI status: {e}", "critical")
            return False

    def test_auto_review_diagnostics(self) -> bool:
        """Test auto-review and approval process with diagnostics."""
        self.log_diagnostic("auto_review", "starting", "Testing auto-review diagnostics")
        
        try:
            # Look for RFC-099 PRs and their review status
            prs = self.gh_json([
                "gh", "pr", "list", "--repo", self.repo,
                "--state", "open", "--json", "number,title,reviewDecision,isDraft"
            ])
            
            rfc_099_prs = [pr for pr in prs if "RFC-099" in pr.get("title", "")]
            
            if not rfc_099_prs:
                self.log_diagnostic("auto_review", "info", "No RFC-099 PRs to review yet")
                return True
            
            for pr in rfc_099_prs:
                pr_number = pr["number"]
                is_draft = pr.get("isDraft", True)
                review_decision = pr.get("reviewDecision", "")
                
                if is_draft:
                    self.log_diagnostic("auto_review", "info", f"PR #{pr_number} still in draft")
                elif review_decision == "APPROVED":
                    self.log_diagnostic("auto_review", "success", f"PR #{pr_number} approved")
                elif review_decision == "REVIEW_REQUIRED":
                    self.log_diagnostic("auto_review", "warning", 
                                      f"PR #{pr_number} still requires review", "warning")
                else:
                    self.log_diagnostic("auto_review", "info", 
                                      f"PR #{pr_number} review status: {review_decision}")
            
            return True
            
        except Exception as e:
            self.log_diagnostic("auto_review", "error", f"Failed to check review status: {e}", "critical")
            return False

    def test_auto_merge_diagnostics(self) -> bool:
        """Test auto-merge process with comprehensive diagnostic analysis."""
        self.log_diagnostic("auto_merge", "starting", "Testing auto-merge diagnostics")
        
        try:
            # Look for RFC-099 PRs and their merge status
            prs = self.gh_json([
                "gh", "pr", "list", "--repo", self.repo,
                "--state", "open", "--json", "number,title,autoMergeRequest,mergeStateStatus,mergeable"
            ])
            
            rfc_099_prs = [pr for pr in prs if "RFC-099" in pr.get("title", "")]
            
            if not rfc_099_prs:
                self.log_diagnostic("auto_merge", "info", "No RFC-099 PRs to merge yet")
                return True
            
            for pr in rfc_099_prs:
                pr_number = pr["number"]
                auto_merge = pr.get("autoMergeRequest")
                merge_state = pr.get("mergeStateStatus", "")
                mergeable = pr.get("mergeable", "")
                
                if auto_merge:
                    self.log_diagnostic("auto_merge", "success", f"PR #{pr_number} has auto-merge enabled")
                else:
                    self.log_diagnostic("auto_merge", "warning",
                                      f"PR #{pr_number} does not have auto-merge enabled", "warning")
                
                if merge_state == "BLOCKED":
                    self.log_diagnostic("auto_merge", "warning",
                                      f"PR #{pr_number} is blocked from merging", "warning")
                elif merge_state == "CLEAN":
                    self.log_diagnostic("auto_merge", "success", f"PR #{pr_number} ready to merge")
                
            return True
            
        except Exception as e:
            self.log_diagnostic("auto_merge", "error", f"Failed to check merge status: {e}", "critical")
            return False

    def generate_diagnostic_report(self) -> str:
        """Generate comprehensive diagnostic report."""
        report = ["", "🔍 DIAGNOSTIC WORKFLOW AUTOMATION TEST REPORT", "=" * 60]
        
        report.append(f"Repository: {self.repo}")
        report.append(f"Timestamp: {self.test_results['timestamp']}")
        report.append("")
        
        # Stage-by-stage analysis
        report.append("📋 Stage Analysis:")
        for stage, info in self.test_results["stages"].items():
            status_icon = "✅" if info["status"] == "success" else "⚠️" if info["status"] == "warning" else "❌"
            report.append(f"  {status_icon} {stage}: {info['message']}")
        
        # Blocker analysis
        report.append("")
        if self.test_results["blockers"]:
            report.append("🚨 Blockers Detected:")
            for blocker in self.test_results["blockers"]:
                level_icon = "⚠️" if blocker["level"] == "warning" else "🚫"
                report.append(f"  {level_icon} [{blocker['stage']}] {blocker['message']}")
        else:
            report.append("✅ No blockers detected")
        
        # Overall assessment
        report.append("")
        critical_blockers = [b for b in self.test_results["blockers"] if b["level"] == "critical"]
        if critical_blockers:
            report.append("🚫 OVERALL STATUS: BLOCKED")
            report.append("❌ Critical blockers prevent complete automation")
        elif self.test_results["blockers"]:
            report.append("⚠️ OVERALL STATUS: PARTIAL SUCCESS")
            report.append("⚠️ Some warnings detected but automation can proceed")
        else:
            report.append("🎉 OVERALL STATUS: SUCCESS")
            report.append("✅ Complete diagnostic workflow automation validated")
            
        report.append("=" * 60)
        return "\n".join(report)

    def run_complete_test(self) -> bool:
        """Run the complete diagnostic workflow test."""
        print("🧪 Testing RFC-099-01: Complete Diagnostic Workflow Automation")
        print("=" * 60)
        
        stages = [
            ("Issue Assignment", self.test_issue_assignment_diagnostics),
            ("PR Creation", self.test_pr_creation_diagnostics), 
            ("CI Diagnostics", self.test_ci_diagnostics),
            ("Auto Review", self.test_auto_review_diagnostics),
            ("Auto Merge", self.test_auto_merge_diagnostics)
        ]
        
        results = []
        for stage_name, test_func in stages:
            print(f"\n🔍 Testing {stage_name}...")
            try:
                result = test_func()
                results.append(result)
            except Exception as e:
                self.log_diagnostic(stage_name.lower().replace(" ", "_"), "error", 
                                  f"Test failed with exception: {e}", "critical")
                results.append(False)
        
        # Determine overall success
        critical_blockers = [b for b in self.test_results["blockers"] if b["level"] == "critical"]
        self.test_results["overall_success"] = len(critical_blockers) == 0
        
        # Generate and display report
        report = self.generate_diagnostic_report()
        print(report)
        
        return self.test_results["overall_success"]


def main():
    """Main test function."""
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("❌ Error: REPO or GITHUB_REPOSITORY environment variable required")
        sys.exit(1)
    
    tester = DiagnosticTester(repo)
    success = tester.run_complete_test()
    
    # Write results to file for workflow consumption
    results_file = os.environ.get("GITHUB_STEP_SUMMARY", "/tmp/diagnostic_test_results.json")
    with open(results_file, "w") as f:
        json.dump(tester.test_results, f, indent=2)
    
    if success:
        print("\n🎉 RFC-099-01 Test: PASSED")
        sys.exit(0)
    else:
        print("\n❌ RFC-099-01 Test: FAILED")
        sys.exit(1)


if __name__ == "__main__":
    main()