#!/bin/bash

# Add existing repository issue to GitHub Project v2
# Usage: ./add-issue-to-project.sh <issue_number> <project_number>

ISSUE_NUMBER=${1:-113}
PROJECT_NUMBER=${2:-2}

echo "Adding repository issue #$ISSUE_NUMBER to project #$PROJECT_NUMBER..."

# First, get the project ID
echo "Finding project #$PROJECT_NUMBER..."
PROJECT_DATA=$(gh api graphql -f query='
{
  viewer {
    projectsV2(first: 10) {
      nodes {
        id
        number
        title
        url
      }
    }
  }
}' --jq ".data.viewer.projectsV2.nodes[] | select(.number == $PROJECT_NUMBER)")

if [ -z "$PROJECT_DATA" ]; then
  echo "❌ Project #$PROJECT_NUMBER not found"
  exit 1
fi

PROJECT_ID=$(echo "$PROJECT_DATA" | jq -r '.id')
PROJECT_TITLE=$(echo "$PROJECT_DATA" | jq -r '.title')
PROJECT_URL=$(echo "$PROJECT_DATA" | jq -r '.url')

echo "✅ Target Project: $PROJECT_TITLE"
echo "✅ Project ID: $PROJECT_ID"
echo "✅ Project URL: $PROJECT_URL"

# Get the issue node ID
echo ""
echo "Getting issue #$ISSUE_NUMBER details..."
ISSUE_DATA=$(gh api graphql -f query="
{
  repository(owner: \"ApprenticeGC\", name: \"ithome-ironman-2025\") {
    issue(number: $ISSUE_NUMBER) {
      id
      title
      number
      url
      state
    }
  }
}" --jq '.data.repository.issue')

if [ -z "$ISSUE_DATA" ] || [ "$ISSUE_DATA" = "null" ]; then
  echo "❌ Issue #$ISSUE_NUMBER not found"
  exit 1
fi

ISSUE_NODE_ID=$(echo "$ISSUE_DATA" | jq -r '.id')
ISSUE_TITLE=$(echo "$ISSUE_DATA" | jq -r '.title')
ISSUE_URL=$(echo "$ISSUE_DATA" | jq -r '.url')
ISSUE_STATE=$(echo "$ISSUE_DATA" | jq -r '.state')

echo "✅ Issue: $ISSUE_TITLE"
echo "✅ Issue Node ID: $ISSUE_NODE_ID"
echo "✅ Issue URL: $ISSUE_URL"
echo "✅ Issue State: $ISSUE_STATE"

# Add the issue to the project
echo ""
echo "Adding issue to project..."
RESULT=$(gh api graphql -f query="
mutation {
  addProjectV2ItemById(input: {
    projectId: \"$PROJECT_ID\"
    contentId: \"$ISSUE_NODE_ID\"
  }) {
    item {
      id
      content {
        ... on Issue {
          title
          number
          url
        }
      }
    }
  }
}" --jq '.data.addProjectV2ItemById.item')

if [ -z "$RESULT" ] || [ "$RESULT" = "null" ]; then
  echo "❌ Failed to add issue to project"
  exit 1
fi

echo "✅ Successfully added issue to project!"
echo ""
echo "🎉 Complete!"
echo "📋 Issue: $ISSUE_TITLE (#$ISSUE_NUMBER)"
echo "📊 Project: $PROJECT_TITLE"
echo "🔗 Project URL: $PROJECT_URL"
echo "🔗 Issue URL: $ISSUE_URL"
