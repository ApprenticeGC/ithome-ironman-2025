#!/usr/bin/env python3
"""
Test suite for ensure_automerge_or_comment.py

Run with: python -m pytest test_ensure_automerge.py -v
Or: python test_ensure_automerge.py
"""

import json
import os
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, mock_open, patch

# Add the scripts directory to path so we can import the module
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".github", "scripts"))

from ensure_automerge_or_comment import add_comment, find_pr_number_by_branch, gh_json, main, run, try_enable_automerge


class TestRunFunction(unittest.TestCase):
    """Test the run function that wraps subprocess.run"""

    @patch("subprocess.run")
    def test_run_success(self, mock_subprocess):
        """Test successful command execution"""
        mock_result = Mock()
        mock_result.stdout = "success"
        mock_result.stderr = ""
        mock_subprocess.return_value = mock_result

        result = run(["echo", "test"])
        self.assertEqual(result.stdout, "success")
        mock_subprocess.assert_called_once()

    @patch("subprocess.run")
    def test_run_with_extra_env(self, mock_subprocess):
        """Test run with extra environment variables"""
        mock_result = Mock()
        mock_subprocess.return_value = mock_result

        extra_env = {"TEST_VAR": "test_value"}
        run(["echo", "test"], extra_env=extra_env)

        # Check that extra_env was merged with os.environ
        call_args = mock_subprocess.call_args
        self.assertIn("TEST_VAR", call_args[1]["env"])
        self.assertEqual(call_args[1]["env"]["TEST_VAR"], "test_value")

    @patch("subprocess.run")
    def test_run_failure_raises_exception(self, mock_subprocess):
        """Test that failed commands raise exceptions when check=True"""
        mock_subprocess.side_effect = subprocess.CalledProcessError(1, "cmd", "error")

        with self.assertRaises(subprocess.CalledProcessError):
            run(["failing", "command"])


class TestGhJsonFunction(unittest.TestCase):
    """Test the gh_json function"""

    @patch("ensure_automerge_or_comment.run")
    def test_gh_json_success(self, mock_run):
        """Test successful JSON parsing"""
        mock_run.return_value.stdout = '{"test": "value"}'

        result = gh_json(["gh", "api", "test"])
        self.assertEqual(result, {"test": "value"})

    @patch("ensure_automerge_or_comment.run")
    def test_gh_json_invalid_json(self, mock_run):
        """Test handling of invalid JSON"""
        mock_run.return_value.stdout = "invalid json"

        with self.assertRaises(json.JSONDecodeError):
            gh_json(["gh", "api", "test"])


class TestFindPrNumberByBranch(unittest.TestCase):
    """Test finding PR number by branch name"""

    @patch("ensure_automerge_or_comment.run")
    def test_find_pr_success(self, mock_run):
        """Test successful PR finding"""
        mock_run.return_value.stdout = (
            '[{"number": 123, "headRefName": "feature-branch"}]'
        )

        result = find_pr_number_by_branch("repo", "feature-branch")
        self.assertEqual(result, 123)

    @patch("ensure_automerge_or_comment.run")
    def test_find_pr_not_found(self, mock_run):
        """Test when PR is not found"""
        mock_run.return_value.stdout = (
            '[{"number": 123, "headRefName": "other-branch"}]'
        )

        result = find_pr_number_by_branch("repo", "feature-branch")
        self.assertIsNone(result)

    @patch("ensure_automerge_or_comment.run")
    def test_find_pr_empty_list(self, mock_run):
        """Test with empty PR list"""
        mock_run.return_value.stdout = "[]"

        result = find_pr_number_by_branch("repo", "feature-branch")
        self.assertIsNone(result)


class TestTryEnableAutomerge(unittest.TestCase):
    """Test the try_enable_automerge function"""

    @patch("ensure_automerge_or_comment.run")
    def test_try_enable_automerge_success(self, mock_run):
        """Test successful auto-merge enable"""
        mock_run.return_value.stdout = "PR_123"

        result = try_enable_automerge("repo", 123)
        self.assertTrue(result)

    @patch("ensure_automerge_or_comment.run")
    def test_try_enable_automerge_no_pr_id(self, mock_run):
        """Test when PR ID cannot be retrieved"""
        mock_run.return_value.stdout = ""

        result = try_enable_automerge("repo", 123)
        self.assertFalse(result)

    @patch("ensure_automerge_or_comment.run")
    def test_try_enable_automerge_graphql_failure(self, mock_run):
        """Test GraphQL call failure"""
        # First call returns PR ID, second call (GraphQL) fails
        mock_run.side_effect = [
            Mock(stdout="PR_123"),  # PR ID call
            subprocess.CalledProcessError(
                1, "gh api graphql", "GraphQL error"
            ),  # GraphQL call
        ]

        result = try_enable_automerge("repo", 123)
        self.assertFalse(result)


class TestAddComment(unittest.TestCase):
    """Test the add_comment function"""

    @patch("ensure_automerge_or_comment.run")
    def test_add_comment_success(self, mock_run):
        """Test successful comment addition"""
        add_comment("repo", 123, "Test comment")

        mock_run.assert_called_once()
        args = mock_run.call_args[0][0]
        self.assertIn("gh", args)
        self.assertIn("pr", args)
        self.assertIn("comment", args)
        self.assertIn("Test comment", args)


class TestMainFunction(unittest.TestCase):
    """Test the main function logic"""

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
        },
    )
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_success_flow(self, mock_gh_json):
        """Test successful main flow"""
        mock_gh_json.return_value = {
            "title": "RFC-123: Test PR",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
            "autoMergeRequest": None,
        }

        with patch(
            "ensure_automerge_or_comment.try_enable_automerge", return_value=True
        ):
            main()  # Should not raise exception

    @patch.dict(os.environ, {"REPO": "", "GITHUB_EVENT": "{}"})
    @patch("ensure_automerge_or_comment.REPO", "")
    def test_main_no_repo(self):
        """Test main with missing repo"""
        with self.assertRaises(SystemExit):
            main()

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "failure"}}',
        },
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_non_success_workflow(self):
        """Test main with non-successful workflow"""
        # Should exit early without error
        main()  # Should return normally, not raise SystemExit

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "success", "pull_requests": []}}',
        },
    )
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_no_pr_found(self):
        """Test main when no PR is found"""
        # Should exit early without error
        main()  # Should return normally, not raise SystemExit

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
        },
    )
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_non_copilot_author(self, mock_gh_json):
        """Test main with non-Copilot author"""
        mock_gh_json.return_value = {
            "title": "RFC-123: Test PR",
            "author": {"login": "human-user"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
        }

        # Should exit early without error
        main()  # Should return normally, not raise SystemExit

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
        },
    )
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_no_rfc_title(self, mock_gh_json):
        """Test main with non-RFC title"""
        mock_gh_json.return_value = {
            "title": "Fix bug",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
        }

        # Should exit early without error
        main()  # Should return normally, not raise SystemExit

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
        },
    )
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_draft_pr(self, mock_gh_json):
        """Test main with draft PR"""
        mock_gh_json.return_value = {
            "title": "RFC-123: Test PR",
            "author": {"login": "Copilot"},
            "draft": True,
            "baseRepository": {"nameWithOwner": "test/repo"},
        }

        # Should exit early without error
        main()  # Should return normally, not raise SystemExit

    @patch.dict(
        os.environ,
        {
            "REPO": "test/repo",
            "GITHUB_EVENT": '{"workflow_run": {"conclusion": "success", "pull_requests": [{"number": 123}]}}',
        },
    )
    @patch("ensure_automerge_or_comment.gh_json")
    @patch("ensure_automerge_or_comment.REPO", "test/repo")
    def test_main_automerge_already_enabled(self, mock_gh_json):
        """Test main when auto-merge is already enabled"""
        mock_gh_json.return_value = {
            "title": "RFC-123: Test PR",
            "author": {"login": "Copilot"},
            "draft": False,
            "baseRepository": {"nameWithOwner": "test/repo"},
            "autoMergeRequest": {"enabled": True},
        }

        # Should exit early without error
        main()  # Should return normally, not raise SystemExit


class TestScriptSyntax(unittest.TestCase):
    """Test script syntax validation"""

    def test_script_syntax(self):
        """Test that the script has valid Python syntax"""
        import os
        import py_compile

        script_path = os.path.join(
            os.path.dirname(__file__),
            ".github",
            "scripts",
            "ensure_automerge_or_comment.py",
        )

        # This will raise an exception if there are syntax errors
        py_compile.compile(script_path, doraise=True)


if __name__ == "__main__":
    unittest.main()
