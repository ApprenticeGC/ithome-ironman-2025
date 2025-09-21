#!/usr/bin/env python3
"""Export Notion RFC page metadata for dependency planning."""
from __future__ import annotations

import argparse
import json
import re
from typing import Any, Dict, List, Optional

from generate_micro_issues_from_rfc import get_notion_client
from notion_page_discovery import NotionPageDiscovery


def extract_series_from_title(title: str) -> Optional[str]:
    match = re.search(r"(?:Game|Architecture)[-\s]?RFC[-\s]?(\d{3})", title, re.IGNORECASE)
    if match:
        return f"RFC-{int(match.group(1)):03d}"
    return None


def extract_micro_identifier(title: str) -> Optional[str]:
    match = re.match(r"Game[-\s]?RFC[-\s]?(\d{3})[-\s]?(\d{2})", title, re.IGNORECASE)
    if match:
        return f"GAME-RFC-{int(match.group(1)):03d}-{int(match.group(2)):02d}"
    return None


def fetch_title(discovery: NotionPageDiscovery, page_id: str) -> str:
    try:
        page_info = discovery.notion.get_page(page_id)
        return discovery._extract_title_from_page(page_info)
    except Exception:
        return page_id


def build_metadata(architecture_id: str, implementation_id: str) -> Dict[str, Any]:
    client = get_notion_client()
    discovery = NotionPageDiscovery(client)

    architecture_pages: List[Dict[str, Any]] = []
    for page in discovery.get_child_pages(architecture_id):
        title = page.get("title") or fetch_title(discovery, page["id"])
        series = extract_series_from_title(title)
        architecture_pages.append(
            {
                "id": page["id"],
                "title": title,
                "series": series,
                "architecture_identifier": f"ARCH-{series}" if series else None,
            }
        )

    implementation_pages: List[Dict[str, Any]] = []
    for page_id in discovery.discover_implementation_pages(implementation_id):
        title = fetch_title(discovery, page_id)
        identifier = extract_micro_identifier(title)
        series = extract_series_from_title(title)
        implementation_pages.append(
            {
                "id": page_id,
                "title": title,
                "identifier": identifier,
                "series": series,
            }
        )

    # Build default dependency map (implementation -> matching architecture series)
    series_to_arch = {entry["series"]: entry for entry in architecture_pages if entry.get("series")}
    dependencies: Dict[str, List[str]] = {}
    for impl in implementation_pages:
        ident = impl.get("identifier")
        series = impl.get("series")
        if not ident or not series:
            continue
        arch_entry = series_to_arch.get(series)
        if arch_entry and arch_entry.get("architecture_identifier"):
            dependencies[ident] = [arch_entry["architecture_identifier"]]

    return {
        "architecture_pages": architecture_pages,
        "implementation_pages": implementation_pages,
        "dependencies": dependencies,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export Notion RFC metadata")
    parser.add_argument("--architecture-collection", required=True, help="Notion page ID for architecture RFCs root")
    parser.add_argument(
        "--implementation-collection", required=True, help="Notion page ID for implementation RFCs root"
    )
    parser.add_argument("--output", default="notion_rfc_pages.json", help="Path to write JSON output")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    metadata = build_metadata(args.architecture_collection, args.implementation_collection)
    with open(args.output, "w", encoding="utf-8") as fh:
        json.dump(metadata, fh, indent=2)
    print(json.dumps(metadata, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
