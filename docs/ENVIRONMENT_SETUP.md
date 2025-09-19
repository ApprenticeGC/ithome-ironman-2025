# Environment Setup

This document explains how to configure environment variables for the project.

## Environment Files

- `.env` - Contains actual secrets and configuration (not tracked in git)
- `.env.example` - Template showing required variables (tracked in git)

## Setup Instructions

1. Copy the example file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with your actual values

## Required Variables

### GitHub API Configuration

For the runner usage badge system, you need a GitHub token with specific permissions:

#### For Billing API (Primary Method)
- **GITHUB_TOKEN**: Fine-grained personal access token with **"Plan"** permissions
- Required for endpoints:
  - `GET /users/{username}/settings/billing/actions`
  - `GET /users/{username}/settings/billing/packages`
  - `GET /users/{username}/settings/billing/shared-storage`
  - `GET /users/{username}/settings/billing/usage`

#### Token Creation Steps:
1. Go to GitHub Settings → Developer settings → Personal access tokens → Fine-grained tokens
2. Create new token
3. Select your account in "Resource owner"
4. Under "Account permissions", grant **"Plan"** with **read** access
5. Copy the token to your `.env` file

#### For Workflow Runs (Fallback Method)
- **AUTO_APPROVE_TOKEN**: Standard GitHub token with repository access
- Used when billing API is unavailable

### Notion Integration (Optional)
- **NOTION_TOKEN**: Integration token from https://www.notion.so/my-integrations
- **NOTION_DATABASE_ID**: Database ID for RFC tracking

## Token Permissions Summary

| Method | Token Type | Required Permissions | API Endpoints |
|--------|------------|---------------------|---------------|
| Billing API | Fine-grained PAT | User: Plan (read) | `/users/{user}/settings/billing/*` |
| Workflow Runs | Classic/Fine-grained | Repository: Actions (read) | `/repos/{owner}/{repo}/actions/runs` |

## Security Notes

- Never commit the `.env` file to git
- Rotate tokens regularly
- Use minimal required permissions
- Store tokens securely

## Troubleshooting

### "Resource not accessible by integration" Error
This typically means your token lacks the required "Plan" permissions. Ensure you've:
1. Created a fine-grained personal access token
2. Granted "Plan" permissions with read access
3. Selected the correct account as resource owner

### Token Not Working
1. Verify token is correctly copied to `.env`
2. Check token hasn't expired
3. Confirm permissions are correctly set
4. Test with a simple API call

## Environment Variable Priority

The Python scripts load environment variables in this order:
1. System environment variables (highest priority)
2. Variables from `.env` file
3. Default values (if any)

This allows GitHub Actions to override local settings automatically.
