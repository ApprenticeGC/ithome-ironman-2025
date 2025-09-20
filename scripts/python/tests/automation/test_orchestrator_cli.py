#!/usr/bin/env python3
import os
import pathlib
import sys
from unittest import mock

import pytest

PRODUCTION_DIR = pathlib.Path(__file__).parent.parent / "production"
if str(PRODUCTION_DIR) not in sys.path:
    sys.path.insert(0, str(PRODUCTION_DIR))

import orchestrator_cli


def test_monitor_pr_flow_restores_env(monkeypatch):
    original_repo = os.environ.get("REPO")
    monkeypatch.delenv("REPO", raising=False)

    with mock.patch("monitor_pr_flow.main") as monitor_mock:
        orchestrator_cli.run_monitor("pr-flow", "org/repo", None, None)
        monitor_mock.assert_called_once()

    assert os.environ.get("REPO") == original_repo


def test_monitor_auto_merge_sets_pr_and_event(monkeypatch):
    monkeypatch.setenv("REPO", "existing/repo")
    monkeypatch.delenv("PR_NUMBER", raising=False)
    monkeypatch.delenv("GITHUB_EVENT", raising=False)

    with mock.patch("ensure_automerge_or_comment.main") as auto_merge_mock:
        orchestrator_cli.run_monitor("auto-merge", "target/repo", 42, '{"key": "value"}')
        auto_merge_mock.assert_called_once()

    assert os.environ["REPO"] == "existing/repo"
    assert "PR_NUMBER" not in os.environ
    assert "GITHUB_EVENT" not in os.environ


def test_monitor_shadow_skips_execution(monkeypatch):
    monkeypatch.delenv("REPO", raising=False)

    with mock.patch("monitor_pr_flow.main") as monitor_mock:
        result = orchestrator_cli.run_monitor("pr-flow", "org/repo", None, None, shadow=True)
        monitor_mock.assert_not_called()
        assert result == 0


def test_cleanup_argument_build(monkeypatch):
    with mock.patch("chain_consistency_manager.main") as chain_main:
        orchestrator_cli.run_cleanup(
            repo="org/repo",
            output="/tmp/plan.json",
            max_runs=50,
            destructive=True,
            emit_events=True,
            event_source="workflow:test",
            print_plan=True,
        )
        chain_main.assert_called_once_with(
            [
                "--repo",
                "org/repo",
                "--output",
                "/tmp/plan.json",
                "--max-runs",
                "50",
                "--destructive",
                "--emit-events",
                "--event-source",
                "workflow:test",
                "--print",
            ]
        )
