# Creating GitHub Issues from Templates

This directory contains detailed issue templates for four planned improvements to the fuckinweather-mcp project.

## Issue Templates

1. **issue-01-response-caching.md** - Add response caching to reduce API calls
2. **issue-02-test-coverage.md** - Expand test coverage for controllers and services  
3. **issue-03-multiple-location-formats.md** - Add support for multiple location formats
4. **issue-04-rate-limiting-security.md** - Implement rate limiting and API security

## How to Create Issues

Since this is running in a CI environment without direct GitHub access, issues can be created manually:

### Option 1: GitHub Web Interface

1. Go to https://github.com/idusortus/fuckinweather-mcp/issues/new
2. Copy the content from one of the template files
3. Fill in the title from the `title:` field in the template
4. Paste the content (excluding the YAML frontmatter) into the issue description
5. Add labels as specified in the `labels:` field
6. Create the issue

### Option 2: GitHub CLI (if available)

```bash
# Issue 1: Response Caching
gh issue create \
  --title "Add Response Caching to Reduce API Calls" \
  --body-file .github/ISSUE_TEMPLATES/issue-01-response-caching.md \
  --label "enhancement,performance"

# Issue 2: Test Coverage
gh issue create \
  --title "Expand Test Coverage for Controllers and Services" \
  --body-file .github/ISSUE_TEMPLATES/issue-02-test-coverage.md \
  --label "testing,quality"

# Issue 3: Multiple Location Formats
gh issue create \
  --title "Add Support for Multiple Location Formats" \
  --body-file .github/ISSUE_TEMPLATES/issue-03-multiple-location-formats.md \
  --label "enhancement,feature"

# Issue 4: Rate Limiting and Security
gh issue create \
  --title "Implement Rate Limiting and API Security" \
  --body-file .github/ISSUE_TEMPLATES/issue-04-rate-limiting-security.md \
  --label "security,enhancement"
```

### Option 3: GitHub API

```bash
# Example for Issue 1
curl -X POST \
  -H "Authorization: token YOUR_GITHUB_TOKEN" \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/idusortus/fuckinweather-mcp/issues \
  -d '{
    "title": "Add Response Caching to Reduce API Calls",
    "body": "See .github/ISSUE_TEMPLATES/issue-01-response-caching.md",
    "labels": ["enhancement", "performance"]
  }'
```

## Implementation Order

Based on dependencies and priorities:

1. **Issue #2** (Test Coverage) - Critical foundation
   - Establish testing infrastructure
   - Create safety net for future changes
   - Priority: **Critical**

2. **Issue #1** (Response Caching) - Quick win
   - Performance improvement
   - Reduces API costs
   - Priority: **High**

3. **Issue #3** (Multiple Location Formats) - Feature expansion
   - Broader user base
   - More flexible API
   - Priority: **Medium**

4. **Issue #4** (Rate Limiting & Security) - Production readiness
   - Protect against abuse
   - Production-ready security
   - Priority: **High**

## Summary

Each issue template includes:
- ✅ Detailed description and problem statement
- ✅ Proposed solution with implementation details
- ✅ Code examples and configurations
- ✅ Acceptance criteria (checklist)
- ✅ Testing requirements
- ✅ Documentation updates needed
- ✅ Priority and effort estimates
- ✅ Dependencies and breaking changes

All issues are ready to be created in GitHub!
