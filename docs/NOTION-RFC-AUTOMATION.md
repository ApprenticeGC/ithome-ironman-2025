# Notion RFC Automation System

## Overview

The enhanced RFC automation system bridges the gap between high-level architectural documentation and implementable micro-issues. It supports both file-based and Notion-based RFC processing with comprehensive deduplication and change detection.

## Architecture

### 2-Layer RFC System

1. **Architectural RFCs** (Design Documentation)
   - High-level system design and patterns
   - Stored in Notion as comprehensive design documents
   - Example: RFC-001: GameConsole 4-Tier Service Architecture

2. **Implementation RFCs** (Micro-Issue Ready)
   - Individual Notion pages, each representing one implementable task
   - Format: `Game-RFC-XXX-YY: Task Name`
   - Each page maps 1:1 to a GitHub issue

### Key Components

- **Enhanced Python Script**: `generate_micro_issues_from_rfc.py`
- **Collection Processor**: `generate_micro_issues_collection.py`
- **SQLite Tracking Database**: Prevents duplicates and tracks changes
- **Notion API Integration**: Direct reading from Notion pages
- **Flow RFCs**: Comprehensive system design documentation

## Usage

### Single Page Processing

#### File-based (Traditional)
```bash
python scripts/python/production/generate_micro_issues_from_rfc.py \
  --rfc-path ./docs/game-rfcs/RFC-001-impl.md \
  --owner ApprenticeGC \
  --repo ithome-ironman-2025 \
  --dry-run
```

#### Notion-based (New)
```bash
python scripts/python/production/generate_micro_issues_from_rfc.py \
  --notion-page-id 2722b68a-e800-812d-a440-d487142573e2 \
  --owner ApprenticeGC \
  --repo ithome-ironman-2025 \
  --dry-run
```

### Collection Processing

#### Process Multiple Files
```bash
python scripts/python/production/generate_micro_issues_collection.py \
  --file-paths file1.md file2.md file3.md \
  --output-format summary
```

#### Discover Files by Pattern
```bash
python scripts/python/production/generate_micro_issues_collection.py \
  --discover-files ".:game-rfc" \
  --output-format summary
```

#### Process Notion Page Collection
```bash
python scripts/python/production/generate_micro_issues_collection.py \
  --notion-pages page-id-1 page-id-2 page-id-3 \
  --output-format json
```

#### Detect Changes in Notion Pages
```bash
python scripts/python/production/generate_micro_issues_collection.py \
  --detect-changes page-id-1 page-id-2 \
  --output-format summary
```

## Features

### Deduplication System

- **SQLite Database**: Tracks all processed pages and created issues
- **Content Hashing**: Detects meaningful content changes
- **GitHub Search Integration**: Prevents duplicate issues via GitHub API
- **Force Mode**: `--force` flag to bypass duplicate detection

### Enhanced Issue Management

- **Rich Metadata**: Issues include RFC identifiers, complexity estimates
- **Smart Assignment**: Supports multiple assignment strategies
- **Sequential Processing**: Only first issue gets assigned for ordered execution
- **Progress Tracking**: Links issues back to Notion pages

### Change Detection

- **Content Normalization**: Ignores formatting-only changes
- **Hash Comparison**: Efficient change detection using SHA-256
- **Incremental Processing**: Only process changed content
- **State Tracking**: Maintains history in SQLite database

## SQLite Database & GitHub Workflow Integration

### Database Storage Strategy

The SQLite tracking database prevents duplicate issue creation and tracks content changes. Here's how it integrates with different environments:

#### Local Development
```bash
# Database created locally
python scripts/python/production/generate_micro_issues_from_rfc.py \
  --notion-page-id xxx --owner owner --repo repo \
  --db-path ./rfc_tracking.db  # Local SQLite file
```

#### GitHub Actions (Recommended)
The database is stored as a GitHub Actions artifact between runs:

```yaml
# Download previous state
- name: Download tracking database
  uses: actions/download-artifact@v4
  with:
    name: rfc-tracking-db
    path: .
  continue-on-error: true

# ... process RFCs ...

# Save state for next run
- name: Upload tracking database
  uses: actions/upload-artifact@v4
  with:
    name: rfc-tracking-db
    path: ./rfc_tracking.db
    retention-days: 90
```

#### Production Scale (Optional)
For high-volume usage, use external database:
```bash
DATABASE_URL=postgresql://user:pass@host:5432/rfc_tracking  # pragma: allowlist secret
```

### Automatic Collection Processing

The system can automatically discover and process pages under your Implementation RFCs section:

```bash
# Auto-discover all Game-RFC pages under Implementation RFCs section
python scripts/python/production/generate_micro_issues_collection.py \
  --notion-collection 2722b68ae8008115b215cfa0c4aa3908 \
  --output-format summary
```

Your Implementation RFCs section ID: `2722b68ae8008115b215cfa0c4aa3908`

## Configuration

### Environment Variables

```bash
# Required for GitHub integration
export GITHUB_TOKEN="ghp_..."

# Required for Notion integration
export NOTION_TOKEN="ntn_..."

# Optional: Default database location
export RFC_TRACKING_DB="./rfc_tracking.db"
```

### Command Line Options

#### Core Options
- `--rfc-path`: Local RFC file path
- `--notion-page-id`: Notion page ID
- `--owner`: GitHub repository owner
- `--repo`: GitHub repository name

#### Processing Options
- `--dry-run`: Show what would be processed without creating issues
- `--force`: Skip duplicate detection
- `--db-path`: SQLite database path (default: `./rfc_tracking.db`)

#### Assignment Options
- `--assign-mode`: `bot`, `user`, `auto` (default: `bot`)

## Database Schema

### Tables

```sql
-- Track Notion page states
CREATE TABLE notion_pages (
    page_id TEXT PRIMARY KEY,
    page_title TEXT NOT NULL,
    last_edited_time TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    rfc_identifier TEXT NOT NULL,
    status TEXT DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Track GitHub issues
CREATE TABLE github_issues (
    issue_number INTEGER PRIMARY KEY,
    issue_title TEXT NOT NULL,
    issue_state TEXT NOT NULL,
    notion_page_id TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (notion_page_id) REFERENCES notion_pages(page_id)
);

-- Processing log
CREATE TABLE processing_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    notion_page_id TEXT NOT NULL,
    action TEXT NOT NULL,
    details TEXT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Implementation RFC Format

### Required Structure

```markdown
# Game-RFC-XXX-YY: Task Title

**Objective**: Clear, single-sentence goal

**Requirements**:
- Specific, testable requirement 1
- Specific, testable requirement 2
- Specific, testable requirement 3

**Implementation Details**:
- Technical approach guidance
- Specific frameworks/patterns to use
- Code organization guidelines

**Acceptance Criteria**:
- [ ] Specific, testable outcome 1
- [ ] Specific, testable outcome 2
- [ ] Build/test requirements
```

### Quality Standards

#### Good Examples
- **Objective**: "Create Tier 1 audio service interface"
- **Requirement**: "Define IAudioService interface with PlayAsync, StopAsync methods"
- **Acceptance**: "Code compiles without warnings using `dotnet build --warnaserror`"

#### Poor Examples
- **Objective**: "Work on audio stuff" (too vague)
- **Requirement**: "Make audio work" (not specific)
- **Acceptance**: "Code works" (not testable)

## Created Implementation RFCs

Successfully created 6 implementation RFC pages in Notion:

1. **Game-RFC-001-01**: Create Tier 1 Base Interfaces
2. **Game-RFC-001-02**: Implement Service Registry Pattern
3. **Game-RFC-001-03**: Create Tier 1 Audio Service Interface
4. **Game-RFC-001-04**: Create Tier 1 Input Service Interface
5. **Game-RFC-001-05**: Create Tier 1 Graphics Service Interface
6. **Game-RFC-001-06**: Create Tier 2 Engine Service Interfaces

Each page follows the standardized format and is ready for GitHub issue generation.

## Workflow

### Development Workflow

1. **Architectural Design**: Create high-level RFCs in Notion
2. **Implementation Planning**: Break down into individual implementation pages
3. **Issue Generation**: Use automation script to create GitHub issues
4. **Development**: GitHub Copilot/agents implement the tasks
5. **Progress Tracking**: Monitor completion via GitHub/Notion integration

### Automation Workflow

1. **Change Detection**: Script monitors Notion pages for modifications
2. **Deduplication**: Database prevents duplicate issue creation
3. **Issue Creation**: GitHub issues created with rich metadata
4. **Assignment**: Smart assignment to appropriate agents/reviewers
5. **Tracking**: All relationships recorded in SQLite database

## Troubleshooting

### Common Issues

#### "Page not found" Error
- Ensure Notion pages are shared with the integration
- Verify page ID format (with or without dashes)
- Check NOTION_TOKEN environment variable

#### "Missing GitHub Token" Error
- Set GITHUB_TOKEN or GH_TOKEN environment variable
- For dry-run testing, the script handles missing tokens gracefully

#### "No micro-issues found" Error
- Verify file contains proper `### Game-RFC-XXX-YY:` headers
- Check for proper format in Notion page titles
- Ensure content follows the required structure

### Debug Mode

Add debug output by modifying the NotionClient to print request details:
```python
print(f"DEBUG: Requesting URL: {url}", file=sys.stderr)
print(f"DEBUG: Headers: {self.headers}", file=sys.stderr)
```

## Future Enhancements

- **Webhook Integration**: Automatic triggering on Notion page updates
- **ML-Based Assignment**: Improve assignment accuracy using machine learning
- **Advanced Workflow**: Support for complex multi-stage issue workflows
- **Integration Hub**: Connect with additional tools (Jira, Linear, etc.)
- **Progress Dashboard**: Real-time visibility into RFC implementation progress
