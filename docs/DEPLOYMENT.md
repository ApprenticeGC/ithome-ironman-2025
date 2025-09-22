# GameConsole Deployment Guide

## Overview

The GameConsole deployment pipeline automates the creation and distribution of deployable packages for the multi-component game console system.

## Deployment Pipeline Features

### Automated CI/CD Pipeline
- **Continuous Integration**: Builds and tests all components in Release configuration
- **Continuous Deployment**: Creates deployment packages and manages releases
- **Multi-Environment Support**: Staging and production environment configurations
- **Artifact Generation**: Creates both individual component packages and complete deployment packages

### Package Types

#### 1. Deployment Archives (.tar.gz)
Individual component packages:
- `GameConsole.Engine.Core.tar.gz` - Main game engine
- `GameConsole.Core.Registry.tar.gz` - Component registry system
- `GameConsole.Plugins.Core.tar.gz` - Plugin architecture
- `GameConsole.Plugins.Lifecycle.tar.gz` - Plugin lifecycle management
- `GameConsole.Input.Services.tar.gz` - Input handling services
- `GameConsole.Graphics.Services.tar.gz` - Graphics rendering services
- `GameConsole.Libraries.tar.gz` - Core abstractions and libraries

Complete deployment:
- `GameConsole-Complete-Deployment.tar.gz` - All components bundled together

#### 2. NuGet Packages (.nupkg)
Available for tagged releases only:
- GameConsole.Core.Abstractions
- GameConsole.Audio.Core
- GameConsole.Graphics.Core
- GameConsole.Input.Core
- GameConsole.Engine.Core
- GameConsole.Plugins.Core

## Deployment Triggers

### Automatic Deployment
- **Main Branch**: Triggers deployment to staging environment on every push to main
- **Tagged Releases**: Creates GitHub releases with all artifacts when pushing version tags (e.g., `v1.0.0`)

### Manual Deployment
Use GitHub Actions workflow dispatch to deploy to specific environments:
1. Navigate to Actions → CD workflow
2. Click "Run workflow"
3. Select target environment (staging/production)
4. Run workflow

## Environment Configuration

### Staging Environment
- Triggered by: Push to main branch or manual dispatch
- Artifacts: Deployment packages only
- Target: Development and testing deployment targets

### Production Environment
- Triggered by: Manual dispatch or version tags
- Artifacts: Deployment packages + NuGet packages + GitHub release
- Target: Production deployment targets

## Usage Instructions

### Deploying from Artifacts

1. **Download Artifacts**: Download the desired deployment package from GitHub Actions artifacts or releases
2. **Extract Package**: Extract the `.tar.gz` file to your deployment directory
3. **Configure Environment**: Set up any environment-specific configurations
4. **Deploy Components**: Deploy individual components or use the complete package

### Using NuGet Packages

For development dependencies:
```bash
dotnet add package GameConsole.Core.Abstractions --version [VERSION]
dotnet add package GameConsole.Engine.Core --version [VERSION]
# Add other packages as needed
```

### Creating a Release

To create a new release with full deployment artifacts:

1. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. The pipeline will automatically:
   - Build all components in Release mode
   - Run all tests
   - Create deployment packages
   - Generate NuGet packages
   - Create GitHub release with artifacts
   - Upload all packages as release assets

## Pipeline Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Source Code   │ -> │   Build & Test   │ -> │   Package Gen   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                         │
┌─────────────────┐    ┌──────────────────┐             │
│   Deployment    │ <- │   Environment    │ <-----------┘
│   Artifacts     │    │   Configuration  │
└─────────────────┘    └──────────────────┘
```

## Technical Details

### Build Configuration
- Target Framework: .NET 8.0
- Build Configuration: Release
- Warnings as Errors: Enabled
- Multi-Project Solution: 17 projects total

### Artifact Retention
- Deployment artifacts: 30 days
- NuGet packages: 90 days
- Release assets: Permanent (via GitHub releases)

### Dependencies
- .NET 8.0 SDK
- Ubuntu latest runner
- Standard GitHub Actions (checkout, setup-dotnet, upload-artifact, create-release)

## Troubleshooting

### Build Failures
- Check that all tests pass locally: `dotnet test ./dotnet --no-build`
- Verify Release configuration builds: `dotnet build ./dotnet -c Release -warnaserror`

### Missing Artifacts
- Check workflow run logs in GitHub Actions
- Verify trigger conditions (branch/tag requirements)
- Ensure all dependencies are restored properly

### Deployment Issues
- Verify environment configuration
- Check artifact extraction and permissions
- Validate target environment compatibility

## Security Considerations

- Artifacts are generated in isolated environments
- No sensitive data is included in packages
- Release assets are publicly accessible via GitHub
- Production deployments should validate artifact integrity