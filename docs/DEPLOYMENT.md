# Deployment Pipeline

This document describes the automated deployment pipeline for the GameConsole Framework.

## Overview

The deployment pipeline automatically builds, packages, and deploys the .NET GameConsole Framework components as NuGet packages.

## Triggers

### Automatic Deployment
- **Git Tags**: Push tags matching `v*` pattern (e.g., `v1.0.0`, `v2.1.3-beta`)
  ```bash
  git tag v1.0.0
  git push origin v1.0.0
  ```

### Manual Deployment
- **Workflow Dispatch**: Manually trigger via GitHub Actions UI
  - Requires version input (e.g., `1.0.0-preview`)
  - Useful for testing or deploying specific versions

## Pipeline Steps

1. **Build & Test**
   - Restore dependencies
   - Build in Release configuration
   - Run all tests

2. **Package**
   - Create NuGet packages for all library projects
   - Excludes test projects automatically
   - Version determined by tag or manual input

3. **Deploy**
   - Publish packages to GitHub Packages
   - Create GitHub Release (for tagged versions)
   - Upload packaged artifacts

## Published Packages

The following NuGet packages are published:

| Package | Description |
|---------|-------------|
| `GameConsole.Core.Abstractions` | Core interfaces and abstractions |
| `GameConsole.Core.Registry` | Service registry and dependency injection |
| `GameConsole.Engine.Core` | Main engine components |
| `GameConsole.Audio.Core` | Audio system abstractions |
| `GameConsole.Graphics.Core` | Graphics system core |
| `GameConsole.Graphics.Services` | Graphics service implementations |
| `GameConsole.Input.Core` | Input system abstractions |
| `GameConsole.Input.Services` | Input service implementations |
| `GameConsole.Plugins.Core` | Plugin system core |
| `GameConsole.Plugins.Lifecycle` | Plugin lifecycle management |

## Usage

### Installing Packages

```bash
# Install core abstractions
dotnet add package GameConsole.Core.Abstractions --version 1.0.0

# Install full engine
dotnet add package GameConsole.Engine.Core --version 1.0.0
```

### Package Source Configuration

Add GitHub Packages as a NuGet source:

```bash
dotnet nuget add source https://nuget.pkg.github.com/ApprenticeGC/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_TOKEN
```

## Development Workflow

1. **Development**: Work in feature branches
2. **Testing**: CI runs on all PRs
3. **Release**: Create and push version tag
4. **Deployment**: Pipeline automatically deploys tagged versions

## Versioning

- Follow [Semantic Versioning](https://semver.org/)
- Use git tags for version identification
- Support pre-release versions (e.g., `1.0.0-beta`, `2.0.0-preview`)

## Troubleshooting

### Package Publication Issues
- Ensure GitHub token has `packages:write` permission
- Verify package source configuration
- Check for duplicate package versions

### Build Failures
- Review build logs in GitHub Actions
- Ensure all tests pass locally
- Verify .NET SDK compatibility

## Configuration

Package metadata is configured in `dotnet/Directory.Build.props`:
- Author information
- License and project URLs
- Package descriptions and tags
- Version handling