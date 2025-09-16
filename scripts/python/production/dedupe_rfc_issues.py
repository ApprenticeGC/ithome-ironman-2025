#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import re
import subprocess
import sys

RFC_RX = re.compile(r"RFC-(\d{3})-(\d{2})", re.IGNORECASE)


def run_gh_json(args: list[str]) -> list | dict | None:
    res = subprocess.run(["gh"] + args, capture_output=True, text=True)
    if res.returncode != 0:
        sys.stderr.write(res.stderr)
        return None
    return json.loads(res.stdout) if res.stdout.strip() else None


def run_gh(args: list[str]) -> int:
    return subprocess.run(["gh"] + args, capture_output=True, text=True).returncode


def main() -> int:
    repo = os.environ.get("REPO") or os.environ.get("GITHUB_REPOSITORY")
    if not repo:
        print("missing REPO env", file=sys.stderr)
        return 2

    issues = (
        run_gh_json(
            [
                "issue",
                "list",
                "--repo",
                repo,
                "--state",
                "open",
                "--limit",
                "500",
                "--json",
                "number,title,assignees",
            ]
        )
        or []
    )
    # Map issue number -> is referenced by an open PR body/title
    pr_ref: set[int] = set()
    prs = (
        run_gh_json(
            [
                "pr",
                "list",
                "--repo",
                repo,
                "--state",
                "open",
                "--json",
                "number,title,body",
            ]
        )
        or []
    )
    for pr in prs:
        text = (pr.get("title") or "") + "\n" + (pr.get("body") or "")
        for m in re.finditer(
            r"(?i)(close[sd]?|fixe?[sd]?|resolve[sd]?)\s*#(\d+)", text
        ):
            try:
                pr_ref.add(int(m.group(2)))
            except Exception:
                pass
    groups: dict[str, list[dict]] = {}
    for it in issues:
        title = it.get("title") or ""
        m = RFC_RX.search(title)
        if not m:
            continue
        key = f"{m.group(1)}-{m.group(2)}"  # RFC-XXX-YY key
        groups.setdefault(key, []).append({"num": int(it["number"]), "title": title})

    actions: list[tuple[int, int]] = []  # (close_num, keep_num)
    for key, lst in groups.items():
        if len(lst) <= 1:
            continue
        # prefer issue referenced by an open PR
        keep = None
        for it in lst:
            if it["num"] in pr_ref:
                keep = it["num"]
                break
        if keep is None:
            lst.sort(key=lambda x: x["num"])
            keep = lst[0]["num"]
        for d in lst[1:]:
            actions.append((d["num"], keep))

    if not actions:
        print("no duplicates found")
        return 0

    # Close duplicates with a comment
    for close_num, keep_num in actions:
        msg = f"Deduplicated: closing in favor of #{keep_num} (same RFC micro)."
        run_gh(["issue", "comment", str(close_num), "--repo", repo, "--body", msg])
        run_gh(["issue", "close", str(close_num), "--repo", repo])
        print(f"closed #{close_num} (keep #{keep_num})")

    return 0


if __name__ == "__main__":
    sys.exit(main())
