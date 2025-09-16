#!/bin/bash
# Script to re-enable automation workflows after validation fixes are tested

echo "Re-enabling automation workflows..."

# List of workflows that were disabled
DISABLED_WORKFLOWS=(
    "auto-ready-pr.yml"
    "auto-approve-merge.yml"
    "auto-advance-micro.yml"
)

cd "$(dirname "$0")/.."

for workflow in "${DISABLED_WORKFLOWS[@]}"; do
    disabled_file=".github/workflows/${workflow}.disabled"
    enabled_file=".github/workflows/${workflow}"

    if [ -f "$disabled_file" ]; then
        echo "Re-enabling: $workflow"
        mv "$disabled_file" "$enabled_file"
    else
        echo "Warning: $disabled_file not found"
    fi
done

echo "✅ Automation workflows re-enabled"
echo "⚠️  Remember to test the enhanced CI validation before using!"
