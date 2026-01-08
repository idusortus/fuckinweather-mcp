#!/bin/bash

# Script to create GitHub issues from templates
# Usage: ./create_issues.sh

REPO="idusortus/fuckinweather-mcp"

echo "Creating GitHub issues for fuckinweather-mcp improvements..."
echo ""

# Issue 1: Response Caching
echo "Creating Issue 1: Response Caching..."
gh issue create \
  --repo "$REPO" \
  --title "Add Response Caching to Reduce API Calls" \
  --body-file .github/ISSUE_TEMPLATES/issue-01-response-caching.md \
  --label "enhancement,performance"

# Issue 2: Test Coverage
echo "Creating Issue 2: Test Coverage..."
gh issue create \
  --repo "$REPO" \
  --title "Expand Test Coverage for Controllers and Services" \
  --body-file .github/ISSUE_TEMPLATES/issue-02-test-coverage.md \
  --label "testing,quality"

# Issue 3: Multiple Location Formats
echo "Creating Issue 3: Multiple Location Formats..."
gh issue create \
  --repo "$REPO" \
  --title "Add Support for Multiple Location Formats" \
  --body-file .github/ISSUE_TEMPLATES/issue-03-multiple-location-formats.md \
  --label "enhancement,feature"

# Issue 4: Rate Limiting and Security
echo "Creating Issue 4: Rate Limiting and Security..."
gh issue create \
  --repo "$REPO" \
  --title "Implement Rate Limiting and API Security" \
  --body-file .github/ISSUE_TEMPLATES/issue-04-rate-limiting-security.md \
  --label "security,enhancement"

echo ""
echo "✅ All four issues created successfully!"
