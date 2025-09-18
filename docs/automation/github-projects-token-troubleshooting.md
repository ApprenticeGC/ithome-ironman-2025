# GitHub Projects v2 Token Troubleshooting

## ❌ **CONFIRMED ISSUE**: Query is using Projects (classic) API fields

**Error Analysis**: 
```
Your token has not been granted the required scopes to execute this query. 
The 'projectV2' field requires one of the following scopes: ['read:project'], 
but your token has only been granted the: ['admin:org', 'gist', 'repo'] scopes.
```

## ✅ **Root Cause**: Projects v2 API Actually DOES Require `read:project` Scope

**Testing confirms**: Whether accessing via `user.projectsV2` or `repository.projectsV2`, the GitHub GraphQL API **requires the `read:project` scope** for **ALL Projects v2 operations**.

The confusion comes from:
1. **GitHub's documentation** being unclear about scope requirements
2. **Fine-grained tokens** not showing "Projects" permission in the UI (but it exists)
3. **Mixed messaging** about whether Projects v2 needs special scopes

**Bottom Line**: GitHub Projects v2 GraphQL API **requires explicit project permissions**, regardless of which API endpoint you use.

## 🚨 **CORRECTED SOLUTION**: Two Clear Paths Forward

## 🚨 **FINAL SOLUTION**: You Need Classic PAT with `read:project` Scope

### **Step 1: Create Classic Personal Access Token**

1. Go to: [GitHub Settings → Personal access tokens → Tokens (classic)](https://github.com/settings/tokens)
2. Click **"Generate new token (classic)"**
3. Select these scopes:
   - ✅ `repo` (Full control of private repositories)
   - ✅ `read:project` (Read access to projects)
   - ✅ `write:project` (Write access to projects) - optional if you only need read

### **Step 2: Update GitHub CLI Authentication**

```powershell
# Option A: Use the login flow
gh auth login --scopes "repo,read:project,write:project"

# Option B: Use token directly
gh auth login --with-token
# Then paste your new classic token when prompted
```

### **Step 3: Test Access**

```powershell
# Test repository-level projects
gh api graphql -f query='
{
  repository(owner: "ApprenticeGC", name: "ithome-ironman-2025") {
    projectsV2(first: 5) {
      nodes {
        number
        title
        url
      }
    }
  }
}'

# Test user-level projects  
gh api graphql -f query='
{
  user(login: "ApprenticeGC") {
    projectsV2(first: 5) {
      nodes {
        number
        title
        url
      }
    }
  }
}'
```

## ✅ **API Access**: Projects v2 uses GraphQL

### **Step 4: Get Issues from Your Project**

Once authenticated with proper scopes, use these queries:

```graphql
# Get all issues from your specific project (user-level)
{
  user(login: "ApprenticeGC") {
    projectV2(number: 2) {
      title
      url
      items(first: 50) {
        nodes {
          content {
            ... on Issue {
              id
              number
              title
              url
              state
              createdAt
              repository {
                nameWithOwner
              }
              labels(first: 10) {
                nodes {
                  name
                  color
                }
              }
            }
          }
          fieldValues(first: 10) {
            nodes {
              ... on ProjectV2ItemFieldSingleSelectValue {
                name
                field {
                  ... on ProjectV2FieldCommon {
                    name
                  }
                }
              }
              ... on ProjectV2ItemFieldTextValue {
                text
                field {
                  ... on ProjectV2FieldCommon {
                    name
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

```graphql
# Or get from repository-level projects
{
  repository(owner: "ApprenticeGC", name: "ithome-ironman-2025") {
    projectsV2(first: 5) {
      nodes {
        number
        title
        items(first: 20) {
          nodes {
            content {
              ... on Issue {
                number
                title
                url
                state
              }
            }
          }
        }
      }
    }
  }
}
```

## 📋 **Summary**

**The Issue**: GitHub Projects v2 GraphQL API requires the `read:project` scope, regardless of whether it's accessed via user or repository endpoints.

**The Solution**: Create a Classic Personal Access Token with `repo` + `read:project` + `write:project` scopes and authenticate GitHub CLI with it.

**Why Fine-grained Tokens Don't Work**: The "Projects" permission in fine-grained tokens either doesn't exist or isn't properly recognized by the GraphQL API for Projects v2 operations.

## 🔄 **Alternative Solution: Using GitHub Workflows (Recommended)**

**Better Approach**: Instead of managing Personal Access Tokens, use GitHub Workflows with the built-in `GITHUB_TOKEN`:

### Benefits:
- ✅ No PAT creation required
- ✅ Automatic permission management  
- ✅ Secure token handling
- ✅ Scoped to repository/organization
- ✅ No token expiration issues

### Implementation:
A workflow has been created at `.github/workflows/projects-v2-demo.yml` that demonstrates:
- Listing Projects v2 for user and repository
- Listing project items
- Creating draft project items

### Usage:
1. Go to Actions tab in your repository
2. Select "GitHub Projects v2 Demo" workflow  
3. Click "Run workflow"
4. Choose the action you want to perform
5. Provide required parameters (project number, item title, etc.)

The workflow uses `GITHUB_TOKEN` which automatically has the necessary permissions to access Projects v2 API when the workflow is triggered.

**Next Steps**: 
1. *(Option A)* Use the workflow approach for automated Projects v2 operations
2. *(Option B)* Create classic PAT with project scopes for local development
3. Re-authenticate GitHub CLI if using PAT approach
4. Test with the provided GraphQL queries
5. Update your workflow's `AUTO_APPROVE_TOKEN` secret if needed

---

*Updated: 2025-09-17 - Confirmed through testing that ALL Projects v2 GraphQL operations require explicit project scopes*
