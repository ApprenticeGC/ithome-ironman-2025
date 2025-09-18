# Flow-RFC-002: Notion Integration System

- **Start Date**: 2025-09-18
- **RFC Author**: Claude
- **Status**: Draft
- **Type**: Flow/Automation Enhancement
- **Depends On**: Flow-RFC-001

## Summary

This RFC defines the Notion API integration system that enables `generate_micro_issues_from_rfc.py` to directly read implementation RFCs from Notion pages, replacing the current file-based approach with real-time Notion content extraction.

## Motivation

Current limitations of file-based RFC processing:

1. **Manual sync required** between Notion and local files
2. **No real-time updates** when RFCs change in Notion
3. **Version inconsistency** between documentation and automation
4. **Manual export process** creates friction and errors
5. **No change detection** from Notion modifications

## Detailed Design

### Notion API Integration Architecture

```python
# New integration components
class NotionRFCReader:
    """Reads RFC content directly from Notion pages"""

class NotionContentExtractor:
    """Extracts micro-issue sections from Notion page content"""

class NotionChangeDetector:
    """Detects changes in Notion pages for incremental updates"""
```

### Enhanced Script Architecture

```
generate_micro_issues_from_rfc.py (Modified)
├── Input Sources
│   ├── --rfc-path (existing file-based)
│   └── --notion-page-id (new Notion-based)
├── Content Processors
│   ├── parse_micro_sections() (existing)
│   ├── parse_micro_table() (existing)
│   └── parse_notion_content() (new)
└── Output
    └── GitHub Issues (unchanged)
```

### Notion Content Extraction

#### Implementation RFC Page Structure
```markdown
# Game-RFC-001: 4-Tier Implementation

<page metadata - ignored>

### Game-RFC-001-01: Create Tier 1 Base Interfaces
**Objective**: Implement foundational service interface contracts
**Requirements**:
- Create GameConsole.Core.Abstractions project
- Define IService base interface
[... content continues ...]

### Game-RFC-001-02: Implement Service Registry Pattern
**Objective**: Build provider selection system
[... content continues ...]
```

#### Extraction Logic
```python
def parse_notion_content(notion_content: str) -> List[MicroIssue]:
    """
    Parse Notion-formatted content for micro-issue sections.

    Expects H3 headers with format: ### Game-RFC-XXX-YY: Title
    Extracts everything until next H3 or end of content.
    """
    # Convert Notion blocks to markdown
    markdown_content = notion_to_markdown(notion_content)

    # Use existing regex pattern
    MICRO_H3 = re.compile(r"^###\s*(Game-RFC-(\d+)-(\d+))\s*:\s*(.+)$", re.IGNORECASE)

    # Parse sections (reuse existing logic)
    return parse_micro_sections(markdown_content)
```

### Script Modifications

#### New Command Line Interface
```bash
# Existing file-based usage (unchanged)
python generate_micro_issues_from_rfc.py \
  --rfc-path ./docs/game-rfcs/RFC-001-impl.md \
  --owner ApprenticeGC \
  --repo ithome-ironman-2025

# New Notion-based usage
python generate_micro_issues_from_rfc.py \
  --notion-page-id 2722b68ae800819b93a9fa6be418a000 \
  --owner ApprenticeGC \
  --repo ithome-ironman-2025 \
  --notion-token ${NOTION_TOKEN}
```

#### Enhanced Argument Parsing
```python
def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser(description="Generate micro issues from RFC")

    # Input source (mutually exclusive)
    input_group = p.add_mutually_exclusive_group(required=True)
    input_group.add_argument("--rfc-path", help="Local RFC file path")
    input_group.add_argument("--notion-page-id", help="Notion page ID")

    # Notion-specific options
    p.add_argument("--notion-token", help="Notion API token (or use NOTION_TOKEN env var)")

    # Existing options (unchanged)
    p.add_argument("--owner", required=True)
    p.add_argument("--repo", required=True)
    p.add_argument("--assign-mode", choices=["bot", "user", "auto"], default="bot")
    p.add_argument("--dry-run", action="store_true")
```

### Notion API Client Implementation

#### Core Client
```python
import requests
from typing import Dict, Any, Optional

class NotionClient:
    def __init__(self, token: str):
        self.token = token
        self.base_url = "https://api.notion.com/v1"
        self.headers = {
            "Authorization": f"Bearer {token}",
            "Notion-Version": "2022-06-28",
            "Content-Type": "application/json"
        }

    def get_page(self, page_id: str) -> Dict[str, Any]:
        """Fetch page metadata and properties"""
        response = requests.get(
            f"{self.base_url}/pages/{page_id}",
            headers=self.headers
        )
        response.raise_for_status()
        return response.json()

    def get_page_content(self, page_id: str) -> Dict[str, Any]:
        """Fetch page content blocks"""
        response = requests.get(
            f"{self.base_url}/blocks/{page_id}/children",
            headers=self.headers
        )
        response.raise_for_status()
        return response.json()
```

#### Content Extraction
```python
class NotionContentExtractor:
    def __init__(self, client: NotionClient):
        self.client = client

    def extract_rfc_content(self, page_id: str) -> str:
        """Extract RFC content as markdown-like text"""
        page_data = self.client.get_page_content(page_id)
        blocks = page_data.get("results", [])

        content_parts = []
        for block in blocks:
            content_parts.append(self._block_to_text(block))

        return "\n".join(content_parts)

    def _block_to_text(self, block: Dict[str, Any]) -> str:
        """Convert Notion block to markdown-like text"""
        block_type = block.get("type")

        if block_type == "heading_3":
            text = self._extract_rich_text(block["heading_3"]["rich_text"])
            return f"### {text}"
        elif block_type == "paragraph":
            text = self._extract_rich_text(block["paragraph"]["rich_text"])
            return text
        elif block_type == "bulleted_list_item":
            text = self._extract_rich_text(block["bulleted_list_item"]["rich_text"])
            return f"- {text}"
        # Add more block types as needed

        return ""

    def _extract_rich_text(self, rich_text_array: list) -> str:
        """Extract plain text from Notion rich text array"""
        return "".join(item.get("plain_text", "") for item in rich_text_array)
```

### Error Handling & Validation

#### Notion API Errors
```python
def handle_notion_errors(func):
    """Decorator for handling common Notion API errors"""
    def wrapper(*args, **kwargs):
        try:
            return func(*args, **kwargs)
        except requests.exceptions.HTTPError as e:
            if e.response.status_code == 401:
                raise RuntimeError("Invalid Notion token")
            elif e.response.status_code == 404:
                raise RuntimeError("Notion page not found or not accessible")
            elif e.response.status_code == 429:
                raise RuntimeError("Notion API rate limit exceeded")
            else:
                raise RuntimeError(f"Notion API error: {e}")
        except requests.exceptions.RequestException as e:
            raise RuntimeError(f"Network error accessing Notion: {e}")
    return wrapper
```

#### Content Validation
```python
def validate_implementation_rfc(content: str) -> bool:
    """Validate that content follows implementation RFC format"""
    # Check for required H3 sections with Game-RFC pattern
    MICRO_H3 = re.compile(r"^###\s*Game-RFC-(\d+)-(\d+)\s*:", re.IGNORECASE | re.MULTILINE)
    matches = MICRO_H3.findall(content)

    if not matches:
        raise ValueError("No Game-RFC micro-issues found in content")

    # Validate sequential numbering
    prev_micro = 0
    for rfc_num, micro_num in matches:
        micro_int = int(micro_num)
        if micro_int != prev_micro + 1:
            raise ValueError(f"Non-sequential micro-issue numbering: expected {prev_micro + 1}, got {micro_int}")
        prev_micro = micro_int

    return True
```

## Implementation Plan

### Phase 1: Core Integration
1. Add Notion API client to script dependencies
2. Implement basic page content extraction
3. Add command-line option for Notion page ID input

### Phase 2: Content Processing
1. Implement Notion block to markdown conversion
2. Integrate with existing micro-issue parsing logic
3. Add content validation for implementation RFC format

### Phase 3: Error Handling
1. Add comprehensive error handling for Notion API
2. Implement retry logic for transient failures
3. Add validation for content format and structure

### Phase 4: Testing & Validation
1. Test with sample implementation RFCs in Notion
2. Validate generated issues match file-based output
3. Performance testing with larger RFC collections

## Configuration

### Environment Variables
```bash
# Required for Notion integration
export NOTION_TOKEN="ntn_..."

# Optional: default page for testing
export DEFAULT_NOTION_RFC_PAGE="2722b68ae800819b93a9fa6be418a000"
```

### Dependencies
```python
# Add to requirements
requests>=2.28.0
python-dotenv>=0.19.0  # For .env file support
```

## Success Metrics

- **API Reliability**: >99% successful Notion API calls
- **Content Accuracy**: 100% parity between Notion and file-based parsing
- **Error Handling**: Graceful handling of all Notion API error conditions
- **Performance**: <5 second response time for typical RFC page processing

## Future Enhancements

- **Bulk processing**: Process multiple Notion pages in parallel
- **Webhook integration**: Automatic triggering on Notion page updates
- **Caching**: Cache Notion content to reduce API calls
- **Content diff**: Detect specific changes between Notion page versions
