#!/usr/bin/env python3
"""
Unit tests for the RFC-102-01 project board integration test script.
"""

import unittest
from unittest.mock import patch, MagicMock, call
import sys
import os

# Add the production directory to path so we can import the test script
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "production"))

try:
    import test_project_board_integration as test_module
except ImportError as e:
    print(f"Could not import test module: {e}")
    sys.exit(1)


class TestProjectBoardIntegration(unittest.TestCase):
    """Test cases for the project board integration test script."""

    @patch('test_project_board_integration.subprocess.run')
    def test_run_gh_command_success(self, mock_run):
        """Test successful GitHub CLI command execution."""
        mock_run.return_value = MagicMock(
            stdout="test output",
            returncode=0
        )
        
        result = test_module.run_gh_command(["issue", "list"])
        
        self.assertEqual(result, "test output")
        mock_run.assert_called_once()

    @patch('test_project_board_integration.subprocess.run')
    def test_run_gh_command_failure(self, mock_run):
        """Test GitHub CLI command failure handling."""
        from subprocess import CalledProcessError
        
        mock_run.side_effect = CalledProcessError(
            returncode=1,
            cmd=["gh", "issue", "list"],
            stderr="Error message"
        )
        
        result = test_module.run_gh_command(["issue", "list"])
        
        self.assertIsNone(result)

    @patch('test_project_board_integration.run_gh_command')
    def test_create_test_issue_success(self, mock_run_gh):
        """Test successful test issue creation."""
        mock_run_gh.return_value = "https://github.com/test/repo/issues/123"
        
        result = test_module.create_test_issue("test/repo")
        
        self.assertEqual(result, 123)
        mock_run_gh.assert_called_once_with([
            "issue", "create",
            "--repo", "test/repo",
            "--title", "RFC-102-01: Final Project Board Integration Test",
            "--body", unittest.mock.ANY,
            "--label", "rfc-102"
        ])

    @patch('test_project_board_integration.run_gh_command')
    def test_create_test_issue_failure(self, mock_run_gh):
        """Test test issue creation failure."""
        mock_run_gh.return_value = None
        
        result = test_module.create_test_issue("test/repo")
        
        self.assertIsNone(result)

    def test_validate_project_tracking_comment_success(self):
        """Test successful project tracking comment validation."""
        comments = [{
            "body": """🎯 **Project Tracking**: This RFC has been added to the [Project Board](https://github.com/users/ApprenticeGC/projects/2/views/1)

📊 **Status**: Added to RFC Backlog
🤖 **Next**: Waiting for Copilot implementation
📈 **Track Progress**: Monitor on the project board as this issue moves through the automation pipeline

*This comment was automatically added by the Project Board Integration workflow*"""
        }]
        
        result = test_module.validate_project_tracking_comment(comments)
        
        self.assertTrue(result)

    def test_validate_project_tracking_comment_missing_content(self):
        """Test project tracking comment validation with missing content."""
        comments = [{
            "body": "🎯 **Project Tracking**: Incomplete comment"
        }]
        
        result = test_module.validate_project_tracking_comment(comments)
        
        self.assertFalse(result)

    def test_validate_project_tracking_comment_no_comments(self):
        """Test project tracking comment validation with no relevant comments."""
        comments = [{
            "body": "This is just a regular comment"
        }]
        
        result = test_module.validate_project_tracking_comment(comments)
        
        self.assertFalse(result)

    @patch('test_project_board_integration.run_gh_command')
    def test_get_issue_comments_success(self, mock_run_gh):
        """Test successful issue comments retrieval."""
        mock_run_gh.return_value = '{"comments": [{"body": "Test comment"}]}'
        
        result = test_module.get_issue_comments("test/repo", 123)
        
        self.assertEqual(result, [{"body": "Test comment"}])
        mock_run_gh.assert_called_once_with([
            "issue", "view", "123",
            "--repo", "test/repo",
            "--comments",
            "--json", "comments"
        ])

    @patch('test_project_board_integration.run_gh_command')
    def test_cleanup_test_issue_success(self, mock_run_gh):
        """Test successful test issue cleanup."""
        mock_run_gh.return_value = "Success"
        
        result = test_module.cleanup_test_issue("test/repo", 123)
        
        self.assertTrue(result)
        # Should call both comment and close commands
        self.assertEqual(mock_run_gh.call_count, 2)

    @patch('test_project_board_integration.time.sleep')
    @patch('test_project_board_integration.get_issue_comments')
    def test_wait_for_workflow_completion_success(self, mock_get_comments, mock_sleep):
        """Test successful workflow completion waiting."""
        mock_get_comments.return_value = [{
            "body": "🎯 **Project Tracking**: Test comment"
        }]
        
        result = test_module.wait_for_workflow_completion("test/repo", 123, timeout_minutes=1)
        
        self.assertTrue(result)
        mock_get_comments.assert_called_once_with("test/repo", 123)

    @patch('test_project_board_integration.time.time')
    @patch('test_project_board_integration.time.sleep')
    @patch('test_project_board_integration.get_issue_comments')
    def test_wait_for_workflow_completion_timeout(self, mock_get_comments, mock_sleep, mock_time):
        """Test workflow completion waiting with timeout."""
        # Mock time to simulate timeout
        mock_time.side_effect = [0, 61]  # Start at 0, then exceed 60 second timeout
        mock_get_comments.return_value = []
        
        result = test_module.wait_for_workflow_completion("test/repo", 123, timeout_minutes=1)
        
        self.assertFalse(result)


class TestScriptSyntax(unittest.TestCase):
    """Test script syntax and structure."""

    def test_script_syntax(self):
        """Test that the script has valid Python syntax."""
        import py_compile
        
        script_path = os.path.join(
            os.path.dirname(__file__),
            "..", "production",
            "test_project_board_integration.py"
        )
        
        # This will raise an exception if there are syntax errors
        py_compile.compile(script_path, doraise=True)

    def test_script_has_main_function(self):
        """Test that the script has a main function."""
        self.assertTrue(hasattr(test_module, 'main'))
        self.assertTrue(callable(test_module.main))

    def test_script_has_required_functions(self):
        """Test that the script has all required functions."""
        required_functions = [
            'run_gh_command',
            'create_test_issue',
            'wait_for_workflow_completion',
            'get_issue_comments',
            'validate_project_tracking_comment',
            'cleanup_test_issue',
            'main'
        ]
        
        for func_name in required_functions:
            with self.subTest(function=func_name):
                self.assertTrue(hasattr(test_module, func_name),
                               f"Missing function: {func_name}")
                self.assertTrue(callable(getattr(test_module, func_name)),
                               f"Function not callable: {func_name}")


if __name__ == "__main__":
    unittest.main()