#!/usr/bin/env bash

# Test script for RFC cleanup duplicates workflow
# This script simulates the workflow logic to test duplicate detection and cleanup

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test data - simulate PRs with RFC titles
TEST_PRS='[
  {"number": 80, "title": "RFC-093-01: Implement agent flow smoke test"},
  {"number": 81, "title": "RFC-093-02: Add recovery mechanisms"},
  {"number": 82, "title": "RFC-094-01: Implement flow status tracking"},
  {"number": 83, "title": "RFC-095-01: Add reset proof functionality"},
  {"number": 84, "title": "RFC-095-02: Handle edge cases in reset"},
  {"number": 85, "title": "RFC-095-03: Add comprehensive testing"},
  {"number": 86, "title": "Regular PR: Update documentation"}
]'

echo -e "${BLUE}🧪 Testing RFC Cleanup Duplicates Logic${NC}"
echo "=========================================="

echo -e "\n${YELLOW}📋 Test Data - Simulated PRs:${NC}"
echo "$TEST_PRS" | jq -r '.[] | "  PR #\(.number): \(.title)"'

echo -e "\n${YELLOW}🔍 Step 1: Find RFC series with multiple open PRs${NC}"

# Simulate the duplicate detection logic from the workflow
DUPLICATE_RFCS=$(echo "$TEST_PRS" | jq -r '[ .[]
                    | select(.title | test("RFC-[0-9]{3}-[0-9]{2}"; "i"))
                    | {num:.number,
                       title:.title,
                       r:(.title|capture("RFC-(?<r>[0-9]{3})-(?<m>[0-9]{2})").r|tonumber),
                       m:(.title|capture("RFC-(?<r>[0-9]{3})-(?<m>[0-9]{2})").m|tonumber)} ]
                  | group_by(.r)
                  | map(select(length > 1))
                  | map({rfc: .[0].r, prs: .})
                  | @json')

echo "Duplicate RFC series found:"
echo "$DUPLICATE_RFCS" | jq '.'

echo -e "\n${YELLOW}🧹 Step 2: Simulate cleanup process${NC}"

# Process each duplicate RFC series
echo "$DUPLICATE_RFCS" | jq -c '.[]' | while read -r rfc_data; do
    RFC_NUM=$(echo "$rfc_data" | jq -r '.rfc')
    echo -e "\n${BLUE}Processing RFC-$RFC_NUM with multiple PRs:${NC}"

    # Get all PRs for this RFC, sorted by micro number
    PRS=$(echo "$rfc_data" | jq -r '.prs | sort_by(.m)')

    # Keep the first (lowest) micro number, remove the rest
    FIRST_PR=$(echo "$PRS" | jq -r '.[0]')
    PR_TO_KEEP=$(echo "$FIRST_PR" | jq -r '.num')
    TITLE_TO_KEEP=$(echo "$FIRST_PR" | jq -r '.title')

    echo -e "  ✅ ${GREEN}Keeping:${NC} PR #$PR_TO_KEEP: $TITLE_TO_KEEP"

    # Process PRs to remove (all except the first)
    echo "$PRS" | jq -c '.[1:][]' | while read -r pr_data; do
        PR_NUM=$(echo "$pr_data" | jq -r '.num')
        PR_TITLE=$(echo "$pr_data" | jq -r '.title')
        MICRO_NUM=$(echo "$pr_data" | jq -r '.m')

        echo -e "  ❌ ${RED}Removing:${NC} PR #$PR_NUM: $PR_TITLE"

        # Simulate cleanup actions
        echo -e "    📝 Close PR #$PR_NUM with comment"
        echo -e "    🗑️  Delete branch for PR #$PR_NUM"
        echo -e "    🔄 Recreate issue for RFC-$RFC_NUM-$MICRO_NUM (unassigned)"
    done
done

echo -e "\n${GREEN}✅ Test completed successfully!${NC}"

# Test edge cases
echo -e "\n${YELLOW}🧪 Testing Edge Cases${NC}"
echo "========================"

# Test with no duplicates
echo -e "\n${BLUE}Test 1: No duplicates${NC}"
NO_DUPS='[
  {"number": 80, "title": "RFC-093-01: Implement agent flow smoke test"},
  {"number": 82, "title": "RFC-094-01: Implement flow status tracking"}
]'

DUPS_TEST=$(echo "$NO_DUPS" | jq -r '[ .[]
                    | select(.title | test("RFC-[0-9]{3}-[0-9]{2}"; "i"))
                    | {num:.number,
                       title:.title,
                       r:(.title|capture("RFC-(?<r>[0-9]{3})-(?<m>[0-9]{2})").r|tonumber),
                       m:(.title|capture("RFC-(?<r>[0-9]{3})-(?<m>[0-9]{2})").m|tonumber)} ]
                  | group_by(.r)
                  | map(select(length > 1))
                  | length')

if [ "$DUPS_TEST" -eq 0 ]; then
    echo -e "  ✅ ${GREEN}PASS:${NC} No duplicates detected"
else
    echo -e "  ❌ ${RED}FAIL:${NC} False positive - duplicates detected when none exist"
fi

# Test with single RFC having many duplicates
echo -e "\n${BLUE}Test 2: Single RFC with many duplicates${NC}"
MANY_DUPS='[
  {"number": 80, "title": "RFC-093-01: Base implementation"},
  {"number": 81, "title": "RFC-093-02: Add features"},
  {"number": 82, "title": "RFC-093-03: Bug fixes"},
  {"number": 83, "title": "RFC-093-04: Documentation"},
  {"number": 84, "title": "RFC-093-05: Testing"}
]'

DUPS_COUNT=$(echo "$MANY_DUPS" | jq -r '[ .[]
                    | select(.title | test("RFC-[0-9]{3}-[0-9]{2}"; "i"))
                    | {num:.number,
                       title:.title,
                       r:(.title|capture("RFC-(?<r>[0-9]{3})-(?<m>[0-9]{2})").r|tonumber),
                       m:(.title|capture("RFC-(?<r>[0-9]{3})-(?<m>[0-9]{2})").m|tonumber)} ]
                  | group_by(.r)
                  | map(select(length > 1))
                  | length')

if [ "$DUPS_COUNT" -eq 1 ]; then
    echo -e "  ✅ ${GREEN}PASS:${NC} Single RFC with multiple PRs detected"
else
    echo -e "  ❌ ${RED}FAIL:${NC} Expected 1 duplicate series, got $DUPS_COUNT"
fi

echo -e "\n${GREEN}🎉 All tests completed!${NC}"
