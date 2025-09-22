# Deployment Pipeline

This directory contains the automated deployment pipeline for the GameConsole libraries.

## Overview

The deployment pipeline provides automated build, test, package, and deployment capabilities for all GameConsole library projects using the Nuke build system.

## Build System

### Nuke Build Targets

- **Clean**: Removes build artifacts and prepares for fresh build
- **Restore**: Restores NuGet dependencies
- **Compile**: Builds all projects with version injection
- **Test**: Runs all unit tests  
- **Pack**: Creates NuGet packages for all libraries
- **Publish**: Publishes packages to configured feeds (GitHub Packages)
- **Deploy**: Complete deployment pipeline

### Local Usage

```bash
# Run from repository root
cd build/nuke

# Show available targets
./build.sh --help

# Build and test
./build.sh Compile

# Create packages
./build.sh Pack

# Full deployment pipeline (CI only)
./build.sh Deploy
```

## GitHub Actions Workflow

The deployment pipeline is automated via `.github/workflows/deployment-pipeline.yml`:

### Triggers

- **Manual**: Via workflow_dispatch with configurable version and target
- **Main branch**: Automatically creates packages on push to main
- **Tags**: Full deployment on version tags (v1.0.0, etc.)
- **Pull Requests**: Build and test validation

### Artifacts

- **NuGet Packages**: All library packages (`.nupkg`)
- **Symbol Packages**: Debug symbols (`.snupkg`)
- **GitHub Releases**: Automatic releases for tagged versions
- **GitHub Packages**: Automatic publishing to GitHub NuGet feed

## Package Metadata

All packages include:
- Consistent versioning across all libraries
- MIT license
- Repository links
- Symbol packages for debugging
- Proper dependency references

## Version Management

- **Manual runs**: Use specified version
- **Tagged releases**: Extract from tag (v1.0.0 → 1.0.0)  
- **Development**: Timestamp-based versions (1.0.0-YYYYMMDDHHMMSS)

## Configuration

Package metadata is configured in `dotnet/Directory.Build.props`:
- Package descriptions and tags
- License and repository information
- Symbol package generation
- Test project exclusions