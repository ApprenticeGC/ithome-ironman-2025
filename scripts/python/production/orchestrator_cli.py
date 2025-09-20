#!/usr/bin/env python3
"""Unified workflow orchestration CLI (Flow-RFC-013).

Provides thin wrappers around existing automation entrypoints so that
workflows can invoke a single tool with subcommands instead of bespoke
scripts. This helps consolidate logic while we gradually refactor the
underlying implementations.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
from contextlib import contextmanager
from typing import Dict, Iterable, List, Optional


@contextmanager
def patched_environ(updates: Dict[str, Optional[str]]):
    """Temporarily patch environment variables."""
    original: Dict[str, Optional[str]] = {}
    try:
        for key, value in updates.items():
            original[key] = os.environ.get(key)
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        yield
    finally:
        for key, value in updates.items():
            if original.get(key) is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = original[key]  # type: ignore[index]


def _load_event_json(event_json: Optional[str]) -> Optional[str]:
    if not event_json:
        return None
    # Normalise whitespace so we do not pass multi-line strings with CRLF noise.
    try:
        parsed = json.loads(event_json)
    except json.JSONDecodeError:
        # Already JSON-like but not parseable (e.g., raw GitHub payload); pass through.
        return event_json
    return json.dumps(parsed, separators=(",", ":"))


def run_monitor(
    target: str,
    repo: Optional[str],
    pr_number: Optional[int],
    event_json: Optional[str],
    *,
    shadow: bool = False,
) -> int:
    if shadow:
        descriptor = f"target={target} repo={repo or os.environ.get('REPO', '<unset>')}"
        print(f"[shadow] monitor run skipped ({descriptor})")
        return 0

    if target == "auto-merge":
        from ensure_automerge_or_comment import main as ensure_automerge_main

        env_updates: Dict[str, Optional[str]] = {}
        if repo:
            env_updates["REPO"] = repo
        if pr_number is not None:
            env_updates["PR_NUMBER"] = str(pr_number)
        if event_json:
            env_updates["GITHUB_EVENT"] = _load_event_json(event_json)

        with patched_environ(env_updates):
            ensure_automerge_main()
        return 0

    if target == "pr-flow":
        from monitor_pr_flow import main as monitor_pr_flow_main

        env_updates = {"REPO": repo} if repo else {}
        with patched_environ(env_updates):
            monitor_pr_flow_main()
        return 0

    raise ValueError(f"Unknown monitor target: {target}")


def run_approve(repo: Optional[str]) -> int:
    from auto_approve_or_dispatch import main as approve_main

    env_updates = {"REPO": repo} if repo else {}
    with patched_environ(env_updates):
        approve_main()
    return 0


def run_diagnose(repo: Optional[str], summary_path: Optional[str]) -> int:
    from test_diagnostic_workflow import main as diagnostic_main

    env_updates: Dict[str, Optional[str]] = {}
    if repo:
        env_updates["REPO"] = repo
    if summary_path:
        env_updates["GITHUB_STEP_SUMMARY"] = summary_path
    with patched_environ(env_updates):
        return diagnostic_main()


def build_chain_args(
    repo: Optional[str],
    output: Optional[str],
    max_runs: Optional[int],
    destructive: bool,
    emit_events: bool,
    event_source: Optional[str],
    print_plan: bool,
) -> List[str]:
    args: List[str] = []
    if repo:
        args.extend(["--repo", repo])
    if output:
        args.extend(["--output", output])
    if max_runs is not None:
        args.extend(["--max-runs", str(max_runs)])
    if destructive:
        args.append("--destructive")
    if emit_events:
        args.append("--emit-events")
    if event_source:
        args.extend(["--event-source", event_source])
    if print_plan:
        args.append("--print")
    return args


def run_cleanup(
    repo: Optional[str],
    output: Optional[str],
    max_runs: Optional[int],
    destructive: bool,
    emit_events: bool,
    event_source: Optional[str],
    print_plan: bool,
) -> int:
    from chain_consistency_manager import main as chain_main

    chain_args = build_chain_args(repo, output, max_runs, destructive, emit_events, event_source, print_plan)
    return chain_main(chain_args)


def create_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Unified workflow orchestrator")
    subparsers = parser.add_subparsers(dest="command", required=True)

    monitor_parser = subparsers.add_parser("monitor", help="Run monitoring routines")
    monitor_parser.add_argument("--target", choices=["pr-flow", "auto-merge"], default="pr-flow")
    monitor_parser.add_argument("--repo", help="Override repository (owner/name)")
    monitor_parser.add_argument("--pr-number", type=int, help="Explicit PR number (auto-merge)")
    monitor_parser.add_argument("--event-json", help="Raw event payload to pass to the monitor")
    monitor_parser.add_argument("--shadow", action="store_true", help="Run monitor in log-only shadow mode")

    approve_parser = subparsers.add_parser("approve", help="Auto-approve pending workflows")
    approve_parser.add_argument("--repo", help="Override repository (owner/name)")

    diagnose_parser = subparsers.add_parser("diagnose", help="Run diagnostic workflow checks")
    diagnose_parser.add_argument("--repo", help="Override repository (owner/name)")
    diagnose_parser.add_argument("--summary-path", help="Path for the diagnostic summary output")

    cleanup_parser = subparsers.add_parser("cleanup", help="Manage chain consistency cleanup")
    cleanup_parser.add_argument("--repo", help="Repository owner/name")
    cleanup_parser.add_argument("--output", help="Destination for generated plan JSON")
    cleanup_parser.add_argument("--max-runs", type=int, help="Maximum workflow runs to inspect")
    cleanup_parser.add_argument("--destructive", action="store_true", help="Execute cleanup actions")
    cleanup_parser.add_argument("--emit-events", action="store_true", help="Emit chain broken events when flagged")
    cleanup_parser.add_argument("--event-source", help="Source identifier for emitted events")
    cleanup_parser.add_argument("--print", dest="print_plan", action="store_true", help="Print plan to stdout")

    return parser


def dispatch(args: argparse.Namespace) -> int:
    if args.command == "monitor":
        return run_monitor(args.target, args.repo, args.pr_number, args.event_json, shadow=args.shadow)
    if args.command == "approve":
        return run_approve(args.repo)
    if args.command == "diagnose":
        return run_diagnose(args.repo, args.summary_path)
    if args.command == "cleanup":
        return run_cleanup(
            args.repo,
            args.output,
            args.max_runs,
            args.destructive,
            args.emit_events,
            args.event_source,
            args.print_plan,
        )
    raise ValueError(f"Unsupported command: {args.command}")


def main(argv: Optional[Iterable[str]] = None) -> int:
    parser = create_parser()
    parsed = parser.parse_args(list(argv) if argv is not None else None)
    return dispatch(parsed)


if __name__ == "__main__":
    sys.exit(main())
