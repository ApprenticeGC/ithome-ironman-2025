# Deployment Pipeline

This directory contains deployment automation for the GameConsole project.

## Overview

The deployment pipeline automates the release process for GameConsole libraries through GitHub Actions workflows.

## Components

### Workflows

- **deployment-pipeline.yml** - Main deployment workflow that:
  - Triggers after successful CI completion
  - Builds release versions of all libraries
  - Packages libraries as NuGet packages
  - Deploys to staging environment
  - Optionally deploys to production with approval
  - Includes health checks and rollback capabilities

### Scripts

- **deployment-health-check.sh** - Health check script that validates:
  - Package presence and integrity
  - Core library availability
  - Environment-specific readiness

## Usage

### Automatic Deployment

The pipeline automatically triggers when:
- CI workflow completes successfully on main branch
- All tests pass
- Build succeeds with warnings as errors

### Manual Deployment

You can manually trigger deployment:

1. Go to GitHub Actions → deployment-pipeline
2. Click "Run workflow"
3. Select environment (staging/production)
4. Optionally force deployment even if CI failed

### Environment Support

- **Staging**: Fast deployment for testing and validation
- **Production**: Blue-green deployment with additional health checks

## Deployment Artifacts

The pipeline creates NuGet packages for all GameConsole libraries:
- GameConsole.Core.Abstractions
- GameConsole.Engine.Core
- GameConsole.Audio.Core
- GameConsole.Graphics.Core
- GameConsole.Input.Core
- GameConsole.Plugins.Core
- GameConsole.Graphics.Services
- GameConsole.Input.Services
- GameConsole.Core.Registry
- GameConsole.Plugins.Lifecycle

## Health Checks

Each deployment includes health checks:
- Package integrity validation
- Core library presence verification
- Environment-specific readiness checks
- Automatic rollback on failure

## Rollback

Automatic rollback occurs when:
- Health checks fail
- Deployment errors are detected

Manual rollback available via workflow dispatch.