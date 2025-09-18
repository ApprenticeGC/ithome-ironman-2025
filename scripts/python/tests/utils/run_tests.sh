#!/usr/bin/env bash

# Test runner for RFC cleanup tests
# This script runs both the Python unit tests and shell script tests

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"

echo -e "${BLUE}🧪 RFC Cleanup Test Runner${NC}"
echo "=========================="
echo "Project root: $PROJECT_ROOT"
echo "Script dir: $SCRIPT_DIR"

# Check if Python is available
if ! command -v python3 &> /dev/null; then
    echo -e "${RED}❌ Python3 not found. Please install Python 3.${NC}"
    exit 1
fi

# Run Python tests
echo -e "\n${YELLOW}🐍 Running Python unit tests...${NC}"
cd "$SCRIPT_DIR"
if python3 test_rfc_cleanup.py; then
    echo -e "${GREEN}✅ Python tests passed${NC}"
else
    echo -e "${RED}❌ Python tests failed${NC}"
    exit 1
fi

# Run shell script tests if available
SHELL_TEST="$SCRIPT_DIR/test-rfc-cleanup.sh"
if [ -f "$SHELL_TEST" ]; then
    echo -e "\n${YELLOW}🐚 Running shell script tests...${NC}"
    if bash "$SHELL_TEST"; then
        echo -e "${GREEN}✅ Shell tests passed${NC}"
    else
        echo -e "${RED}❌ Shell tests failed${NC}"
        exit 1
    fi
else
    echo -e "\n${YELLOW}⚠️  Shell test script not found: $SHELL_TEST${NC}"
fi

echo -e "\n${GREEN}🎉 All tests completed successfully!${NC}"
