#!/usr/bin/env python3
"""
Notion page discovery utilities for automatic collection processing.
"""

import json
import sys
from typing import Any, Dict, List

from generate_micro_issues_from_rfc import NotionClient, notion_token


class NotionPageDiscovery:
    """Discover and categorize Notion pages for processing"""

    def __init__(self, notion_client: NotionClient):
        self.notion = notion_client

    def get_child_pages(self, parent_page_id: str) -> List[Dict[str, Any]]:
        """Get all child pages under a parent page"""
        try:
            # Get blocks from the parent page
            content = self.notion.get_page_content(parent_page_id)
            blocks = content.get("results", [])

            child_pages = []
            for block in blocks:
                if block.get("type") == "child_page":
                    # This is a child page block
                    child_pages.append(
                        {"id": block["id"], "title": block.get("child_page", {}).get("title", ""), "type": "child_page"}
                    )
                elif block.get("type") == "link_to_page":
                    # This is a link to another page
                    page_ref = block.get("link_to_page", {})
                    if page_ref.get("type") == "page_id":
                        page_id = page_ref["page_id"]
                        try:
                            page_info = self.notion.get_page(page_id)
                            title = self._extract_title_from_page(page_info)
                            child_pages.append({"id": page_id, "title": title, "type": "linked_page"})
                        except Exception:
                            continue

            return child_pages

        except Exception as e:
            print(f"Error getting child pages for {parent_page_id}: {e}", file=sys.stderr)
            return []

    def discover_implementation_pages(self, implementation_section_id: str) -> List[str]:
        """Discover all Game-RFC implementation pages under the Implementation RFCs section"""
        child_pages = self.get_child_pages(implementation_section_id)

        implementation_page_ids = []
        for page in child_pages:
            title = page.get("title", "")
            # Check if this looks like an implementation RFC
            if title.startswith("Game-RFC-") and ":" in title:
                implementation_page_ids.append(page["id"])

        return implementation_page_ids

    def categorize_rfcs(self, rfc_root_id: str) -> Dict[str, List[str]]:
        """Categorize all RFCs under the root into Architecture vs Implementation"""
        child_pages = self.get_child_pages(rfc_root_id)

        categories = {
            "architecture_section": None,
            "implementation_section": None,
            "architecture_pages": [],
            "implementation_pages": [],
        }

        # Find the Architecture and Implementation sections
        for page in child_pages:
            title = page.get("title", "").lower()
            if "architecture" in title and "rfc" in title:
                categories["architecture_section"] = page["id"]
            elif "implementation" in title and "rfc" in title:
                categories["implementation_section"] = page["id"]

        # Get pages from each section
        if categories["architecture_section"]:
            arch_pages = self.get_child_pages(categories["architecture_section"])
            categories["architecture_pages"] = [p["id"] for p in arch_pages]

        if categories["implementation_section"]:
            impl_pages = self.get_child_pages(categories["implementation_section"])
            # Filter for Game-RFC pattern
            for page in impl_pages:
                title = page.get("title", "")
                if title.startswith("Game-RFC-") and ":" in title:
                    categories["implementation_pages"].append(page["id"])

        return categories

    def _extract_title_from_page(self, page_info: Dict[str, Any]) -> str:
        """Extract title from page metadata"""
        try:
            title_prop = page_info.get("properties", {}).get("title", {})
            if title_prop.get("type") == "title":
                return "".join(item.get("plain_text", "") for item in title_prop.get("title", []))
            return f"Page {page_info.get('id', 'unknown')}"
        except Exception:
            return "Unknown Page"


def main():
    """CLI for testing page discovery"""
    import argparse

    parser = argparse.ArgumentParser(description="Discover Notion RFC pages")
    parser.add_argument("--rfc-root", required=True, help="Root RFC page ID")
    parser.add_argument("--implementation-section", help="Implementation RFCs section ID")
    parser.add_argument(
        "--action", choices=["categorize", "list-implementation"], default="categorize", help="Action to perform"
    )

    args = parser.parse_args()

    # Initialize Notion client
    token = notion_token()
    notion_client = NotionClient(token)
    discovery = NotionPageDiscovery(notion_client)

    if args.action == "categorize":
        categories = discovery.categorize_rfcs(args.rfc_root)
        print(json.dumps(categories, indent=2))

    elif args.action == "list-implementation":
        if not args.implementation_section:
            print("--implementation-section required for list-implementation action")
            return 1

        impl_pages = discovery.discover_implementation_pages(args.implementation_section)
        print(json.dumps({"implementation_pages": impl_pages}, indent=2))

    return 0


if __name__ == "__main__":
    sys.exit(main())
