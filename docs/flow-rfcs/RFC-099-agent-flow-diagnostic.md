# RFC-099: Agent Flow Diagnostic and Recovery System

- **Start Date**: 2025-09-19
- **RFC Author**: Claude
- **Status**: Active
- **Priority**: Critical

## Summary

This RFC documents the critical issues discovered in the GitHub Copilot agent flow automation system and proposes systematic diagnostics and recovery mechanisms. The current agent flow has multiple failure points that cause orphaned issues, stuck PRs, and empty tracking databases.

## Motivation

The agent flow automation system (issue → PR → runner → merge) has several critical problems:

1. **Merge Conflict Handling**: PRs with merge conflicts get stuck in infinite recreation loops
2. **Empty RFC Tracking Database**: The `rfc_tracking.db` is empty despite being 24KB, indicating data corruption or workflow failures
3. **Orphaned Issues**: Issues like #157 and #145 reference Notion Implementation RFCs that aren't tracked properly
4. **Incomplete Monitoring**: The PR flow monitor doesn't detect merge conflicts before attempting auto-merge

These issues break the entire automation chain and require manual intervention.

## Problems Identified

### 1. Merge Conflict Loop (CRITICAL - FIXED)

**Problem**: `monitor_pr_flow.py` didn't check for merge conflicts before enabling auto-merge
- PRs with `mergeable: "CONFLICTING"` and `mergeStateStatus: "DIRTY"` would fail auto-merge
- Agent watchdog would detect failures and recreate issues infinitely
- Example: PR #190 caused recreation loop with issues #214, #218, #219, #220, #221

**Root Cause**: Missing merge state validation in `scripts/python/production/monitor_pr_flow.py:455-502`

**Solution Applied**: Enhanced monitoring script to:
- Use GraphQL to fetch `mergeable` and `mergeStateStatus` fields
- Detect conflicts early: `if mergeable == "CONFLICTING" or merge_state == "DIRTY"`
- Trigger cleanup/recreation process via existing `cleanup_recreate_issue.py`

### 2. Empty RFC Tracking Database (ACTIVE ISSUE)

**Problem**: `rfc_tracking.db` has 0 records in all tables despite 24KB file size
- Last modified: 2025-09-19 00:29:59
- Tables: `notion_pages`, `github_issues`, `processing_log` all empty
- Indicates RFC automation workflows aren't working properly

**Investigation Needed**:
- RFC sync workflow only triggers on RFC file changes
- Current open issues (#157, #145) reference Notion Implementation RFCs not in repo
- Database may be corrupted or workflows failing silently

### 3. Orphaned Issue Analysis (PARTIALLY RESOLVED)

**Current Open Issues**:
- Issue #157: "GAME-RFC-002-01: Create Audio Category Services" (assigned to Copilot)
- Issue #145: "GAME-RFC-001-05: Create Tier 1 Graphics Service Interface" (assigned to Copilot)

**Current Open PRs**:
- PR #134: Dependabot bump actions/checkout (legitimate, not agent-related)

**Status**: These issues are NOT orphaned - they reference legitimate Implementation RFCs in Notion per `NOTION-RFC-AUTOMATION.md`, but the tracking database isn't populated.

## Detailed Diagnostic Plan

### Phase 1: Database Recovery and Validation

#### Micro RFC-099-01: Investigate RFC Database Corruption
- Analyze why `rfc_tracking.db` is empty despite file size
- Check recent RFC workflow runs for failures
- Validate database schema integrity
- Recover or rebuild tracking data from GitHub issues and Notion

#### Micro RFC-099-02: Validate Notion Integration System
- Test connection to Notion Implementation RFCs
- Verify that issues #157 and #145 correspond to real Notion pages
- Check if Notion sync workflows are functioning
- Document current Implementation RFC status in Notion

### Phase 2: Monitoring Enhancement

#### Micro RFC-099-03: Comprehensive Flow Monitoring
- Enhance `monitor_pr_flow.py` with additional health checks
- Add database connectivity validation
- Monitor RFC automation workflow status
- Create alerting for flow disruptions

#### Micro RFC-099-04: Agent Flow Recovery Automation
- Build automated recovery for stuck chains
- Implement health check endpoints
- Create manual recovery triggers for edge cases
- Add comprehensive logging for flow state changes

### Phase 3: Prevention and Resilience

#### Micro RFC-099-05: Flow State Validation System
- Add pre-flight checks before issue/PR creation
- Validate RFC tracking data before automation
- Implement circuit breakers for failing workflows
- Create rollback mechanisms for corrupted states

## Implementation Status

### ✅ Completed
- **Merge Conflict Detection**: Fixed in `monitor_pr_flow.py` with GraphQL-based conflict detection
- **Problem Analysis**: Documented root causes and current state

### 🔄 In Progress
- **RFC Documentation**: This RFC documents the problems systematically

### ⏳ Pending
- **Database Recovery**: Need to investigate and fix empty RFC tracking database
- **Notion Integration Validation**: Verify Implementation RFCs in Notion
- **Enhanced Monitoring**: Add comprehensive health checks
- **Recovery Automation**: Build automated recovery mechanisms

## Acceptance Criteria

1. **Database Health**: `rfc_tracking.db` populated with current Implementation RFC status
2. **Merge Conflict Prevention**: No more infinite recreation loops due to conflicts
3. **Monitoring Coverage**: Comprehensive health checks for all automation components
4. **Recovery Capability**: Automated recovery from common failure scenarios
5. **Documentation**: Complete diagnostic procedures and troubleshooting guides

## Dependencies

- Notion API access for Implementation RFC validation
- GitHub API access with appropriate permissions
- RFC automation workflow understanding
- Database recovery procedures

## Risk Assessment

**High**: Agent flow is core to development productivity
**Impact**: Manual intervention required for all RFC implementations
**Urgency**: Critical - affecting active development workflow

## Next Actions

1. Investigate RFC database corruption (Micro RFC-099-01)
2. Validate Notion Implementation RFC integration (Micro RFC-099-02)
3. Build comprehensive monitoring (Micro RFC-099-03)
4. Implement recovery automation (Micro RFC-099-04)
5. Add prevention mechanisms (Micro RFC-099-05)