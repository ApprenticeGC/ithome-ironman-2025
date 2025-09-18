# Flow-RFC-005: GitHub Integration Enhancement

- **Start Date**: 2025-09-18
- **RFC Author**: Claude
- **Status**: Draft
- **Type**: Flow/Automation Enhancement
- **Depends On**: Flow-RFC-001, Flow-RFC-002, Flow-RFC-004

## Summary

This RFC defines enhancements to GitHub integration for the Notion RFC automation system, including improved issue management, assignment strategies, PR workflow integration, and monitoring capabilities. It establishes production-ready GitHub automation that supports the full RFC→micro-issue→implementation lifecycle.

## Motivation

Current GitHub integration limitations:

1. **Basic Issue Creation**: Only creates issues without lifecycle management
2. **No Assignment Strategy**: Missing automated assignment to agents/reviewers
3. **Limited Issue Tracking**: No connection between issues and PR completion
4. **No Progress Monitoring**: No visibility into implementation progress
5. **Missing Workflow Integration**: No automation for issue state transitions

## Detailed Design

### Enhanced Issue Management

#### Issue Creation with Metadata
```python
class EnhancedGitHubClient:
    def create_micro_issue(self, micro_issue: MicroIssue, metadata: IssueMetadata) -> GitHubIssue:
        """Create issue with comprehensive metadata and labels"""

        # Generate issue body with enhanced formatting
        body = self._format_enhanced_issue_body(micro_issue)

        # Create issue with metadata
        issue = self.github.create_issue(
            title=micro_issue.title,
            body=body,
            labels=self._generate_labels(micro_issue, metadata),
            assignees=self._determine_assignees(metadata.assignment_mode),
            milestone=self._get_or_create_milestone(metadata.rfc_identifier)
        )

        return issue

    def _format_enhanced_issue_body(self, micro_issue: MicroIssue) -> str:
        """Format issue body with structured sections"""
        return f"""## Objective
{micro_issue.objective}

## Requirements
{self._format_requirements_list(micro_issue.requirements)}

## Implementation Details
{micro_issue.implementation_details}

## Acceptance Criteria
{self._format_acceptance_criteria(micro_issue.acceptance_criteria)}

---
### Automation Metadata
- **RFC**: {micro_issue.rfc_identifier}
- **Generated**: {datetime.now().isoformat()}
- **Type**: Implementation Task
- **Estimated Complexity**: {self._estimate_complexity(micro_issue)}

### Related Links
- [Architecture RFC](link-to-notion-architecture-page)
- [Implementation RFC](link-to-notion-implementation-page)
"""
```

#### Dynamic Label Management
```python
class LabelManager:
    def __init__(self, github_client: GitHubClient):
        self.github = github_client
        self._ensure_labels_exist()

    def _ensure_labels_exist(self):
        """Create standard labels if they don't exist"""
        standard_labels = [
            ("rfc-implementation", "7C4DDB", "RFC implementation task"),
            ("tier-1", "FF6B6B", "Tier 1 (Infrastructure) component"),
            ("tier-2", "4ECDC4", "Tier 2 (Engine) component"),
            ("tier-3", "45B7D1", "Tier 3 (Game) component"),
            ("tier-4", "96CEB4", "Tier 4 (UI) component"),
            ("ai-agent", "FFA07A", "Assigned to AI coding agent"),
            ("needs-review", "FFD93D", "Requires human review"),
            ("complexity-low", "C7ECEE", "Low complexity task"),
            ("complexity-medium", "FFE66D", "Medium complexity task"),
            ("complexity-high", "FF6B6B", "High complexity task"),
        ]

        for name, color, description in standard_labels:
            self._create_label_if_missing(name, color, description)

    def generate_issue_labels(self, micro_issue: MicroIssue, metadata: IssueMetadata) -> List[str]:
        """Generate appropriate labels for micro-issue"""
        labels = ["rfc-implementation"]

        # Add tier-based label
        if "tier" in micro_issue.title.lower():
            tier_match = re.search(r"tier\s*(\d+)", micro_issue.title.lower())
            if tier_match:
                labels.append(f"tier-{tier_match.group(1)}")

        # Add complexity label
        complexity = self._estimate_complexity(micro_issue)
        labels.append(f"complexity-{complexity}")

        # Add assignment mode label
        if metadata.assignment_mode == "ai-agent":
            labels.append("ai-agent")
        elif metadata.assignment_mode == "human":
            labels.append("needs-review")

        return labels
```

### Assignment Strategy System

#### Multi-Mode Assignment
```python
class AssignmentStrategy:
    def __init__(self, config: AssignmentConfig):
        self.config = config
        self.ai_agents = config.ai_agents
        self.human_reviewers = config.human_reviewers

    def determine_assignees(self, micro_issue: MicroIssue, mode: str) -> List[str]:
        """Determine who should be assigned to the issue"""

        if mode == "ai-only":
            return self._assign_to_ai_agent(micro_issue)
        elif mode == "human-only":
            return self._assign_to_human(micro_issue)
        elif mode == "hybrid":
            return self._assign_hybrid(micro_issue)
        elif mode == "auto":
            return self._auto_assign(micro_issue)
        else:
            return []

    def _assign_to_ai_agent(self, micro_issue: MicroIssue) -> List[str]:
        """Assign to appropriate AI coding agent"""
        complexity = self._estimate_complexity(micro_issue)

        if complexity == "low":
            return [self.ai_agents["copilot"]]
        elif complexity == "medium":
            return [self.ai_agents["copilot"], self.ai_agents["gemini"]]
        else:
            # High complexity - assign to human with AI support
            return [self.human_reviewers["lead_dev"]]

    def _auto_assign(self, micro_issue: MicroIssue) -> List[str]:
        """Intelligent assignment based on content analysis"""
        content_analysis = self._analyze_content(micro_issue)

        # Interface/contract work -> AI suitable
        if content_analysis.has_interface_work and not content_analysis.has_complex_logic:
            return [self.ai_agents["copilot"]]

        # Test implementation -> AI suitable
        if content_analysis.is_test_implementation:
            return [self.ai_agents["copilot"]]

        # Complex business logic -> Human review
        if content_analysis.has_complex_logic:
            return [self.human_reviewers["senior_dev"]]

        # Default to AI with human fallback
        return [self.ai_agents["copilot"]]
```

#### Workload Balancing
```python
class WorkloadBalancer:
    def __init__(self, github_client: GitHubClient):
        self.github = github_client

    def get_current_workload(self, assignee: str) -> WorkloadMetrics:
        """Get current workload for assignee"""
        open_issues = self.github.get_issues(
            assignee=assignee,
            state="open",
            labels=["rfc-implementation"]
        )

        return WorkloadMetrics(
            total_issues=len(open_issues),
            complexity_score=sum(self._complexity_score(issue) for issue in open_issues),
            avg_age_days=self._calculate_avg_age(open_issues)
        )

    def balance_assignment(self, candidates: List[str], micro_issue: MicroIssue) -> str:
        """Select best candidate based on current workload"""
        workloads = {candidate: self.get_current_workload(candidate)
                    for candidate in candidates}

        # Select candidate with lowest workload score
        return min(candidates,
                  key=lambda c: workloads[c].get_workload_score())
```

### PR Integration & Lifecycle

#### Issue-PR Linking
```python
class PRWorkflowManager:
    def __init__(self, github_client: GitHubClient, db_path: str):
        self.github = github_client
        self.db = sqlite3.connect(db_path)

    def link_pr_to_issue(self, pr_number: int, issue_number: int):
        """Link PR to its implementing issue"""
        self.db.execute("""
            INSERT OR REPLACE INTO pr_issue_links (pr_number, issue_number, created_at)
            VALUES (?, ?, ?)
        """, (pr_number, issue_number, datetime.now()))

    def auto_detect_pr_issues(self, pr: PullRequest) -> List[int]:
        """Auto-detect issues from PR title/body"""
        issue_patterns = [
            r"(?:close|closes|fix|fixes|resolve|resolves)\s+#(\d+)",
            r"Game-RFC-\d+-\d+",  # Extract from micro-issue identifier
        ]

        text = f"{pr.title} {pr.body}"
        issue_numbers = []

        for pattern in issue_patterns:
            matches = re.finditer(pattern, text, re.IGNORECASE)
            for match in matches:
                if match.group(1).isdigit():
                    issue_numbers.append(int(match.group(1)))

        return issue_numbers

    def update_issue_on_pr_events(self, pr_event: PREvent):
        """Update linked issues based on PR state changes"""
        linked_issues = self._get_linked_issues(pr_event.pr_number)

        for issue_number in linked_issues:
            if pr_event.action == "opened":
                self._add_pr_comment(issue_number, f"PR #{pr_event.pr_number} opened")
                self._add_label(issue_number, "in-progress")

            elif pr_event.action == "merged":
                self._add_pr_comment(issue_number, f"PR #{pr_event.pr_number} merged")
                self._close_issue(issue_number)

            elif pr_event.action == "closed" and not pr_event.merged:
                self._add_pr_comment(issue_number, f"PR #{pr_event.pr_number} closed without merge")
                self._remove_label(issue_number, "in-progress")
```

#### Automated Progress Tracking
```python
class ProgressTracker:
    def __init__(self, github_client: GitHubClient, notion_client: NotionClient, db_path: str):
        self.github = github_client
        self.notion = notion_client
        self.db = sqlite3.connect(db_path)

    def generate_rfc_progress_report(self, rfc_identifier: str) -> ProgressReport:
        """Generate progress report for RFC implementation"""

        # Get all issues for RFC
        issues = self.db.execute("""
            SELECT gi.issue_number, gi.issue_state, np.rfc_identifier
            FROM github_issues gi
            JOIN notion_pages np ON gi.notion_page_id = np.page_id
            WHERE np.rfc_identifier LIKE ?
        """, (f"{rfc_identifier}-%",)).fetchall()

        total_issues = len(issues)
        closed_issues = len([i for i in issues if i[1] == "closed"])

        # Get PR completion data
        pr_data = self._get_pr_completion_data(issues)

        return ProgressReport(
            rfc_identifier=rfc_identifier,
            total_issues=total_issues,
            completed_issues=closed_issues,
            completion_percentage=closed_issues / total_issues * 100,
            avg_completion_time=pr_data.avg_completion_time,
            blocked_issues=self._identify_blocked_issues(issues)
        )

    def update_notion_progress(self, rfc_identifier: str):
        """Update progress in Notion implementation page"""
        report = self.generate_rfc_progress_report(rfc_identifier)

        # Find Notion page for this RFC implementation
        implementation_page = self._find_implementation_page(rfc_identifier)

        if implementation_page:
            progress_section = f"""
## Implementation Progress

- **Total Tasks**: {report.total_issues}
- **Completed**: {report.completed_issues}
- **Progress**: {report.completion_percentage:.1f}%
- **Avg Completion Time**: {report.avg_completion_time}

### Status Breakdown
{self._format_status_breakdown(report)}
"""

            self.notion.update_page_section(implementation_page.id, progress_section)
```

### Monitoring & Analytics

#### Issue Metrics Dashboard
```python
class MetricsDashboard:
    def __init__(self, github_client: GitHubClient, db_path: str):
        self.github = github_client
        self.db = sqlite3.connect(db_path)

    def generate_automation_metrics(self) -> AutomationMetrics:
        """Generate comprehensive automation metrics"""

        # Issue creation metrics
        creation_metrics = self._calculate_creation_metrics()

        # Completion metrics
        completion_metrics = self._calculate_completion_metrics()

        # AI vs Human performance
        performance_metrics = self._calculate_performance_metrics()

        return AutomationMetrics(
            creation_rate=creation_metrics.issues_per_day,
            completion_rate=completion_metrics.completion_percentage,
            avg_completion_time=completion_metrics.avg_time_hours,
            ai_success_rate=performance_metrics.ai_success_rate,
            human_intervention_rate=performance_metrics.human_intervention_rate,
            quality_score=self._calculate_quality_score()
        )

    def _calculate_ai_success_rate(self) -> float:
        """Calculate success rate for AI-assigned issues"""
        ai_issues = self.db.execute("""
            SELECT issue_number, issue_state FROM github_issues
            WHERE issue_number IN (
                SELECT issue_number FROM issue_assignments
                WHERE assignee_type = 'ai'
            )
        """).fetchall()

        if not ai_issues:
            return 0.0

        completed = len([i for i in ai_issues if i[1] == "closed"])
        return completed / len(ai_issues) * 100
```

#### Alert System
```python
class AlertSystem:
    def __init__(self, config: AlertConfig):
        self.config = config
        self.notification_channels = config.notification_channels

    def monitor_issue_health(self):
        """Monitor issue health and send alerts"""

        # Check for stale issues
        stale_issues = self._find_stale_issues()
        if stale_issues:
            self._send_alert("stale_issues", {
                "count": len(stale_issues),
                "issues": [i.number for i in stale_issues]
            })

        # Check for failed AI assignments
        failed_ai_issues = self._find_failed_ai_assignments()
        if failed_ai_issues:
            self._send_alert("ai_assignment_failures", {
                "count": len(failed_ai_issues),
                "issues": [i.number for i in failed_ai_issues]
            })

        # Check automation health
        automation_health = self._check_automation_health()
        if automation_health.score < 0.8:
            self._send_alert("automation_health_degraded", automation_health)

    def _send_alert(self, alert_type: str, data: dict):
        """Send alert through configured channels"""
        for channel in self.notification_channels:
            if channel.type == "slack":
                self._send_slack_alert(channel, alert_type, data)
            elif channel.type == "email":
                self._send_email_alert(channel, alert_type, data)
```

### Command Line Enhancements

#### Enhanced CLI Options
```python
def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser(description="Generate micro issues from Notion RFCs")

    # Input sources
    input_group = p.add_mutually_exclusive_group(required=True)
    input_group.add_argument("--notion-page-id", help="Single Notion page ID")
    input_group.add_argument("--notion-collection", help="Notion collection/database ID")
    input_group.add_argument("--rfc-path", help="Local RFC file path")

    # Processing modes
    p.add_argument("--mode", choices=["single", "incremental", "full-collection"],
                   default="single", help="Processing mode")

    # Assignment options
    p.add_argument("--assign-mode", choices=["ai-only", "human-only", "hybrid", "auto"],
                   default="auto", help="Assignment strategy")
    p.add_argument("--assignee", help="Specific assignee (overrides assign-mode)")

    # Workflow options
    p.add_argument("--create-milestone", action="store_true",
                   help="Create milestone for RFC issues")
    p.add_argument("--link-prs", action="store_true",
                   help="Enable automatic PR-issue linking")

    # Monitoring options
    p.add_argument("--update-progress", action="store_true",
                   help="Update progress in Notion pages")
    p.add_argument("--generate-report", action="store_true",
                   help="Generate progress report")

    # Enhanced existing options
    p.add_argument("--dry-run", action="store_true")
    p.add_argument("--force", action="store_true", help="Skip duplicate detection")
    p.add_argument("--db-path", default="./rfc_tracking.db")
```

## Implementation Strategy

### Phase 1: Enhanced Issue Management
1. Implement enhanced issue creation with metadata
2. Add dynamic label management system
3. Create milestone integration

### Phase 2: Assignment Strategy
1. Build multi-mode assignment system
2. Implement workload balancing
3. Add content analysis for auto-assignment

### Phase 3: PR Integration
1. Implement issue-PR linking
2. Add automated progress tracking
3. Create PR event handlers

### Phase 4: Monitoring & Analytics
1. Build metrics dashboard
2. Implement alert system
3. Add Notion progress updates

## Success Metrics

- **Assignment Accuracy**: >90% appropriate assignments (AI vs human)
- **Automation Coverage**: >95% of issues automatically processed
- **PR Integration**: 100% of PRs correctly linked to issues
- **Progress Tracking**: Real-time progress visibility in Notion
- **Alert Response**: <15 minute alert response time for critical issues

## Configuration Examples

### Assignment Configuration
```yaml
assignment:
  ai_agents:
    copilot: "github-copilot[bot]"
    gemini: "gemini-code-assist[bot]"
  human_reviewers:
    lead_dev: "senior-developer"
    senior_dev: "tech-lead"
  complexity_thresholds:
    low: 0-50
    medium: 51-100
    high: 101+
```

### Alert Configuration
```yaml
alerts:
  channels:
    - type: slack
      webhook_url: "${SLACK_WEBHOOK}"
      channel: "#rfc-automation"
    - type: email
      recipients: ["team@company.com"]

  thresholds:
    stale_issue_days: 7
    ai_failure_rate: 20
    automation_health_min: 0.8
```

## Future Enhancements

- **ML-Based Assignment**: Use machine learning to improve assignment accuracy
- **Predictive Analytics**: Predict issue completion times and bottlenecks
- **Advanced Workflow**: Support for complex multi-stage issue workflows
- **Integration Hub**: Connect with additional tools (Jira, Linear, etc.)
