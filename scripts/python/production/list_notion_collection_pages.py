#!/usr/bin/env python3
"""Helper to list Game-RFC notion sub-pages."""
from __future__ import annotations

import argparse
import os
import sys

from generate_micro_issues_from_rfc import get_notion_client
from notion_page_discovery import NotionPageDiscovery


def main() -> int:
    parser = argparse.ArgumentParser(description="List implementation pages under a Notion collection")
    parser.add_argument("--collection-id", required=True, help="Notion collection page ID")
    args = parser.parse_args()

    token = os.environ.get("NOTION_TOKEN")
    if not token:
        print("NOTION_TOKEN environment variable is required", file=sys.stderr)
        return 2

    client = get_notion_client(token)
    discovery = NotionPageDiscovery(client)
    pages = discovery.discover_implementation_pages(args.collection_id)
    for page_id in pages:
        print(page_id)
    return 0


if __name__ == "__main__":
    sys.exit(main())
