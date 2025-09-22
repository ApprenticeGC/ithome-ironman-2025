# Deployment Pipeline Configuration

This directory contains configuration files for the deployment pipeline automation.

## Environment Configuration

### Staging Environment
- **Purpose**: Pre-production testing and validation
- **Access**: Internal team members
- **Deployment**: Triggered on workflow_dispatch with `staging` environment
- **Rollback**: Automated on failure detection

### Production Environment  
- **Purpose**: Live production releases
- **Access**: Public users and consumers
- **Deployment**: Triggered on git tags (v*) or manual workflow_dispatch with `production` environment
- **Rollback**: Manual process with proper coordination

## Package Publishing

### NuGet Packages
The following packages are published to NuGet.org:
- `GameConsole.Core.Abstractions` - Core abstractions and interfaces
- `GameConsole.Audio.Core` - Audio system functionality
- `GameConsole.Graphics.Core` - Graphics rendering functionality  
- `GameConsole.Input.Core` - Input handling functionality
- `GameConsole.Engine.Core` - Core engine functionality
- `GameConsole.Plugins.Core` - Plugin system functionality
- `GameConsole.Plugins.Lifecycle` - Plugin lifecycle management
- `GameConsole.Core.Registry` - Service registry functionality
- `GameConsole.Graphics.Services` - Graphics services implementation
- `GameConsole.Input.Services` - Input services implementation

### GitHub Releases
- Created automatically for version tags
- Includes NuGet packages as release assets
- Automated changelog generation
- Support for pre-release versions

## Secrets Configuration

The deployment pipeline requires the following secrets:

### Required Secrets
- `NUGET_API_KEY`: API key for publishing to NuGet.org
- `GITHUB_TOKEN`: Automatically provided by GitHub Actions

### Optional Secrets
- `AUTO_APPROVE_PAT`: Personal access token for auto-approval workflows

## Versioning Strategy

### Automatic Versioning
- **Tag-based**: When pushed with git tag (e.g., `v1.2.3`)
- **Commit-based**: Generates version from last tag + commit count + SHA
- **Manual**: Can be specified via workflow_dispatch input

### Version Format
- **Release**: `1.2.3` (semantic versioning)
- **Pre-release**: `1.2.3-beta.1` or `1.2.3.45-abc123` (auto-generated)

## Usage

### Manual Deployment
```bash
# Via GitHub CLI
gh workflow run deployment-pipeline \
  --field environment=staging \
  --field version=1.0.0

# Via GitHub web interface
Actions → Deployment Pipeline → Run workflow
```

### Release Deployment
```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0

# This automatically triggers production deployment
```

## Monitoring and Alerts

The deployment pipeline includes:
- Build and test validation
- Package integrity checks  
- Deployment status notifications
- Failure alert mechanisms
- Rollback procedures

## Troubleshooting

Common issues and solutions:
1. **Build failures**: Check .NET version compatibility and dependencies
2. **Package conflicts**: Verify version numbers and dependencies
3. **Permission errors**: Ensure proper secrets configuration
4. **Deployment failures**: Check environment-specific configurations

For more details, see the workflow logs in GitHub Actions.