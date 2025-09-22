# Deployment Pipeline Guide

## Overview

The GameConsole framework now includes automated deployment pipeline capabilities for releasing NuGet packages and managing releases.

## Components

### 1. Release Workflow (`.github/workflows/release.yml`)

Automated release pipeline that:
- ✅ Validates version format and prerequisites
- ✅ Builds and tests all projects with warnings as errors  
- ✅ Creates NuGet packages for library projects only
- ✅ Creates GitHub releases with changelog
- ✅ Optionally publishes packages to NuGet.org

### 2. Version Management Script (`scripts/release-version.sh`)

Utility script for version management:
- Calculate next version numbers
- Validate version format
- Create and push version tags

### 3. Package Metadata

Updated project files with NuGet package metadata:
- Package descriptions and tags
- Author and copyright information
- Repository links
- Symbol package generation

## Usage

### Automatic Releases (Recommended)

1. **Create a version tag**:
   ```bash
   ./scripts/release-version.sh tag 1.0.0
   ```

2. **Pipeline automatically**:
   - Validates CI status
   - Builds and tests projects
   - Creates NuGet packages
   - Creates GitHub release
   - Uploads packages as release assets

### Manual Release Workflow

Use GitHub Actions workflow dispatch:

1. Go to **Actions** → **Release and Deploy**
2. Click **Run workflow**  
3. Specify:
   - Version (e.g., `1.0.0`)
   - Create GitHub release: `true`
   - Publish to NuGet: `false` (for testing)

## Package Publishing

### Test Publishing (Default)
- Packages are created and attached to GitHub releases
- No automatic NuGet publishing
- Safe for testing and validation

### Production Publishing  
- Requires `NUGET_API_KEY` secret in repository
- Set `publish_packages: true` in workflow dispatch
- Publishes to NuGet.org automatically

## Version Management

### Semantic Versioning
- Format: `MAJOR.MINOR.PATCH`
- Pre-release: `MAJOR.MINOR.PATCH-suffix`

### Version Helpers
```bash
# Check next version
./scripts/release-version.sh next-patch    # 1.0.0 → 1.0.1
./scripts/release-version.sh next-minor    # 1.0.0 → 1.1.0
./scripts/release-version.sh next-major    # 1.0.0 → 2.0.0

# Validate version format
./scripts/release-version.sh validate 1.2.3
```

## Package Structure

The pipeline creates packages for these library projects:
- `GameConsole.Core.Abstractions` - Core interfaces and abstractions
- `GameConsole.Core.Registry` - Service registry and DI
- `GameConsole.Engine.Core` - Core engine components  
- `GameConsole.Audio.Core` - Audio system core
- `GameConsole.Graphics.Core` - Graphics system core
- `GameConsole.Graphics.Services` - Graphics services
- `GameConsole.Input.Core` - Input system core
- `GameConsole.Input.Services` - Input services
- `GameConsole.Plugins.Core` - Plugin system core
- `GameConsole.Plugins.Lifecycle` - Plugin lifecycle management

**Excluded**: Test projects and TestLib are not packaged.

## Security

### Secrets Required
- `NUGET_API_KEY` - For publishing to NuGet.org (production only)

### Safety Features
- Manual approval required for NuGet publishing
- Dry-run mode for testing
- Version validation prevents invalid releases
- CI validation gates prevent broken builds

## Integration

### CI/CD Flow
```
PR → CI Validation → Merge → Tag → Release Pipeline → Deploy
```

### Existing CI Integration
- Builds on existing CI foundation (`ci.yml`)
- Leverages CI validation system (RFC-091)  
- Uses same build and test commands
- Maintains warnings-as-errors enforcement

## Troubleshooting

### Common Issues

**Package creation fails**:
- Ensure project builds successfully
- Check package metadata in `.csproj` files
- Verify version format

**Release workflow fails**:
- Check CI status is green
- Validate version tag format
- Ensure no duplicate version tags

**Publishing fails**:
- Verify `NUGET_API_KEY` secret is set
- Check package doesn't already exist on NuGet
- Ensure package metadata is complete

### Debug Steps

1. **Test packaging locally**:
   ```bash
   dotnet pack ./dotnet --configuration Release --output ./test-packages
   ```

2. **Validate workflow**:
   - Use manual workflow dispatch with test version
   - Check GitHub Actions logs for detailed output

3. **Version management**:
   ```bash
   ./scripts/release-version.sh help
   ```

## Future Enhancements

- Automatic changelog generation from PR titles
- Multi-environment deployments (staging/production)  
- Package dependency validation
- Performance regression testing
- Security vulnerability scanning