#!/usr/bin/env python3
# Ensure production directory on path
import pathlib
import sys
import unittest
from datetime import datetime, timedelta, timezone

PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

import chain_consistency_manager as ccm


class ChainIdentifierTests(unittest.TestCase):
    def test_extract_chain_id_from_title(self):
        self.assertEqual(ccm.extract_chain_id("Game-RFC-001-02: Sample Task"), "RFC-001-02")
        self.assertEqual(ccm.extract_chain_id("RFC-010-07: Another Task"), "RFC-010-07")
        self.assertIsNone(ccm.extract_chain_id("No chain here"))

    def test_extract_chain_id_from_branch(self):
        self.assertEqual(ccm.extract_chain_id_from_branch("copilot/rfc-123-04-awesome"), "RFC-123-04")
        self.assertEqual(ccm.extract_chain_id_from_branch("automation/rfc-5-1-reset"), "RFC-005-01")
        self.assertEqual(ccm.extract_chain_id_from_branch("feature/something"), None)


class ChainStateDetectionTests(unittest.TestCase):
    def setUp(self):
        self.now = datetime.now(timezone.utc)

    def test_branch_only_state(self):
        record = ccm.ChainRecord(
            chain_id="RFC-001-02",
            issues=[ccm.IssueInfo(number=1, title="Game-RFC-001-02: Done", state="closed", assignees=[], url="")],
            branches=[ccm.BranchInfo(name="copilot/rfc-001-02-task", protected=False, url="")],
            pull_requests=[],
            runs=[],
        )
        states = record.detect_states(now=self.now)
        self.assertIn(ccm.STATE_BRANCH_ONLY, states)

    def test_issue_only_assigned_state(self):
        record = ccm.ChainRecord(
            chain_id="RFC-002-03",
            issues=[ccm.IssueInfo(number=2, title="Game-RFC-002-03", state="open", assignees=["bot"], url="")],
        )
        states = record.detect_states(now=self.now)
        self.assertIn(ccm.STATE_ISSUE_ONLY_ASSIGNED, states)

    def test_duplicate_prs_state(self):
        record = ccm.ChainRecord(
            chain_id="RFC-010-05",
            pull_requests=[
                ccm.PullInfo(
                    number=11,
                    title="Game-RFC-010-05",
                    state="open",
                    merged=False,
                    draft=False,
                    head_ref="copilot/rfc-010-05-a",
                    source_branch="copilot/rfc-010-05-a",
                    url="",
                ),
                ccm.PullInfo(
                    number=12,
                    title="Game-RFC-010-05",
                    state="open",
                    merged=False,
                    draft=False,
                    head_ref="copilot/rfc-010-05-b",
                    source_branch="copilot/rfc-010-05-b",
                    url="",
                ),
            ],
        )
        states = record.detect_states(now=self.now)
        self.assertIn(ccm.STATE_DUPLICATE_PRS, states)

    def test_ci_stuck_state(self):
        created_at = (self.now - timedelta(minutes=60)).strftime("%Y-%m-%dT%H:%M:%SZ")
        record = ccm.ChainRecord(
            chain_id="RFC-020-01",
            runs=[
                ccm.RunInfo(
                    run_id=100,
                    workflow_name="ci",
                    head_branch="copilot/rfc-020-01-branch",
                    status="in_progress",
                    conclusion=None,
                    created_at=created_at,
                    url="https://example.com/run/100",
                )
            ],
        )
        states = record.detect_states(now=self.now)
        self.assertIn(ccm.STATE_CI_STUCK, states)

    def test_recommended_actions_dedup(self):
        record = ccm.ChainRecord(chain_id="RFC-030-01")
        actions = record.recommended_actions([ccm.STATE_BRANCH_ONLY, ccm.STATE_BRANCH_ONLY])
        self.assertEqual(len(actions), len(set(actions)))


if __name__ == "__main__":
    unittest.main()
