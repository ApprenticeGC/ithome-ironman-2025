#!/usr/bin/env python3
"""
Test suite for GitHub automation scripts
Run with: python -m pytest scripts/python/tests/test_scripts.py -v
"""
import json
import os
import subprocess
import sys
import tempfile
from unittest.mock import Mock, mock_open, patch

import pytest

# Add the scripts directory to path so we can import the modules
sys.path.insert(
    0,
    os.path.join(
        os.path.dirname(__file__), "..", "..", "..", "scripts", "python", "production"
    ),
)

from ensure_automerge_or_comment import add_comment, find_pr_number_by_branch
from ensure_automerge_or_comment import main as ensure_automerge_main
from ensure_automerge_or_comment import try_enable_automerge


class TestEnsureAutomergeOrComment:
    """Test cases for ensure_automerge_or_comment.py"""

    @patch("ensure_automerge_or_comment.run")
    def test_try_enable_automerge_success(self, mock_run):
        """Test successful auto-merge enable"""
        # Mock successful PR ID retrieval
        mock_run.side_effect = [
            Mock(stdout="PR_kwDOPvdiTc5abc123\n"),  # PR ID
            Mock(),  # GraphQL mutation success
        ]

        result = try_enable_automerge("test/repo", 123)
        assert result is True
        assert mock_run.call_count == 2

    @patch("ensure_automerge_or_comment.run")
    def test_try_enable_automerge_no_pr_id(self, mock_run):
        """Test when PR ID cannot be retrieved"""
        mock_run.return_value = Mock(stdout="\n")  # Empty PR ID

        result = try_enable_automerge("test/repo", 123)
        assert result is False

    @patch("ensure_automerge_or_comment.run")
    def test_try_enable_automerge_graphql_failure(self, mock_run):
        """Test when GraphQL mutation fails"""
        mock_run.side_effect = [
            Mock(stdout="PR_kwDOPvdiTc5abc123\n"),  # PR ID
            subprocess.CalledProcessError(
                1, "cmd", stderr="GraphQL error"
            ),  # GraphQL failure
        ]

        result = try_enable_automerge("test/repo", 123)
        assert result is False

    @patch("ensure_automerge_or_comment.run")
    def test_find_pr_number_by_branch_found(self, mock_run):
        """Test finding PR number by branch name"""
        mock_data = [
            {"number": 123, "headRefName": "feature-branch"},
            {"number": 456, "headRefName": "other-branch"},
        ]
        mock_run.return_value = Mock(stdout=json.dumps(mock_data))

        result = find_pr_number_by_branch("test/repo", "feature-branch")
        assert result == 123

    @patch("ensure_automerge_or_comment.run")
    def test_find_pr_number_by_branch_not_found(self, mock_run):
        """Test when branch is not found"""
        mock_data = [{"number": 123, "headRefName": "other-branch"}]
        mock_run.return_value = Mock(stdout=json.dumps(mock_data))

        result = find_pr_number_by_branch("test/repo", "feature-branch")
        assert result is None

    @patch("ensure_automerge_or_comment.run")
    def test_add_comment(self, mock_run):
        """Test adding comment to PR"""
        add_comment("test/repo", 123, "Test comment")

        mock_run.assert_called_once_with(
            [
                "gh",
                "pr",
                "comment",
                "123",
                "--repo",
                "test/repo",
                "--body",
                "Test comment",
            ],
            check=False,
            extra_env={"GH_TOKEN": ""},
        )

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.try_enable_automerge")
    def test_main_success_flow(self, mock_try_automerge, mock_gh_json):
        """Test successful main flow"""
        mock_gh_json.return_value = {
            "title": "feat(flow): RFC-090 test",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
            "autoMergeRequest": None,
        }
        mock_try_automerge.return_value = True

        ensure_automerge_main()

        mock_try_automerge.assert_called_once_with("test/repo", 123)

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.try_enable_automerge")
    @patch("ensure_automerge_or_comment.add_comment")
    def test_main_automerge_failure(
        self, mock_add_comment, mock_try_automerge, mock_gh_json
    ):
        """Test main flow when auto-merge fails"""
        mock_gh_json.return_value = {
            "title": "feat(flow): RFC-090 test",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
            "autoMergeRequest": None,
        }
        mock_try_automerge.return_value = False

        ensure_automerge_main()

        mock_add_comment.assert_called_once()
        assert "Auto-merge could not be enabled" in mock_add_comment.call_args[0][2]

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "failure"}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_non_success_workflow(self):
        """Test main exits early for non-successful workflows"""
        # Should not raise exception, just return early
        ensure_automerge_main()

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [], "head_branch": "test-branch"}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.find_pr_number_by_branch")
    def test_main_no_pr_found(self, mock_find_pr):
        """Test main when no PR is found"""
        mock_find_pr.return_value = None

        ensure_automerge_main()

        mock_find_pr.assert_called_once_with("test/repo", "test-branch")

        mock_find_pr.assert_called_once()

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.gh_json")
    def test_main_non_copilot_author(self, mock_gh_json):
        """Test main skips for non-Copilot authors"""
        mock_gh_json.return_value = {
            "title": "feat(flow): RFC-090 test",
            "author": {"login": "human-user"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
        }

        ensure_automerge_main()

        # Should not call try_enable_automerge
        mock_gh_json.assert_called_once()

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.gh_json")
    def test_main_no_rfc_title(self, mock_gh_json):
        """Test main skips for titles without RFC-"""
        mock_gh_json.return_value = {
            "title": "feat(flow): regular feature",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
        }

        ensure_automerge_main()

        mock_gh_json.assert_called_once()

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.gh_json")
    def test_main_draft_pr(self, mock_gh_json):
        """Test main skips for draft PRs"""
        mock_gh_json.return_value = {
            "title": "feat(flow): RFC-090 test",
            "author": {"login": "Copilot"},
            "draft": True,
            "baseRepository": {"nameWithOwner": "test/repo"},
        }

        ensure_automerge_main()

        mock_gh_json.assert_called_once()

    @patch(
        "ensure_automerge_or_comment.EVENT_JSON",
        '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    @patch("ensure_automerge_or_comment.gh_json")
    def test_main_automerge_already_enabled(self, mock_gh_json):
        """Test main skips when auto-merge is already enabled"""
        mock_gh_json.return_value = {
            "title": "feat(flow): RFC-090 test",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
            "autoMergeRequest": {"enabled": True},
        }

        ensure_automerge_main()

        mock_gh_json.assert_called_once()


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
