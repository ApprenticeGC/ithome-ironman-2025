# GameConsole Deployment Pipeline

This documentation describes the automated deployment pipeline for the GameConsole project, implementing RFC-012-02.

## Overview

The deployment pipeline provides automated containerization and deployment capabilities for the GameConsole .NET 8 framework. It includes:

- Docker containerization
- Multi-environment deployment (development, staging, production)
- Automated CI/CD workflows
- Infrastructure as Code
- Monitoring and observability

## Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Development   │ -> │     Staging     │ -> │   Production    │
│   Environment   │    │   Environment   │    │  Environment    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         |                       |                       |
         v                       v                       v
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Docker Local   │    │  GitHub Actions │    │  GitHub Actions │
│                 │    │  Auto Deploy    │    │  Manual Deploy  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Components

### 1. Docker Configuration

#### Dockerfile
- Multi-stage build for optimized production images
- Security-focused with non-root user
- Health checks included
- Supports .NET 8 applications

#### docker-compose.yml
- Complete application stack
- Database (PostgreSQL) and caching (Redis)
- Monitoring stack (Prometheus, Grafana) - optional
- Environment-specific configurations

### 2. GitHub Actions Workflows

#### CI Workflow (.github/workflows/ci.yml)
Existing workflow that handles:
- Code compilation with warnings as errors
- Test execution
- Build validation

#### CD Workflow (.github/workflows/cd.yml)
New workflow that handles:
- Automatic deployment triggers after successful CI
- Environment-specific deployment routing
- Manual deployment capabilities

#### Deploy Workflow (.github/workflows/deploy.yml)
Reusable workflow for deployment operations:
- Container image building and pushing
- Multi-environment deployment
- Rollback capabilities
- Health checks and verification

### 3. Environment Management

Configuration files for different environments:
- `deploy/config/development.env` - Local development settings
- `deploy/config/staging.env` - Staging environment settings  
- `deploy/config/production.env` - Production environment settings

### 4. Deployment Scripts

#### deploy.sh
Comprehensive deployment script with:
- Environment validation
- Health checks
- Rollback capabilities
- Dry-run support

## Usage

### Local Development

1. **Start local environment:**
```bash
docker-compose up -d
```

2. **Deploy with script:**
```bash
./deploy/scripts/deploy.sh development deploy latest
```

3. **Check status:**
```bash
./deploy/scripts/deploy.sh development status
```

### Staging Deployment

Staging deployments are automatically triggered when:
- Code is pushed to the `develop` branch
- CI tests pass successfully

Manual staging deployment:
```bash
# Via GitHub Actions workflow_dispatch
# Or using the deployment script
./deploy/scripts/deploy.sh staging deploy v1.2.3
```

### Production Deployment

Production deployments are automatically triggered when:
- Code is pushed to the `main` branch  
- CI tests pass successfully

Manual production deployment (recommended):
```bash
# Via GitHub Actions workflow_dispatch UI
# Select "production" environment and specify image tag
```

### Rollback

If a deployment fails or issues are detected:

1. **Automatic rollback** (production only) - triggers on deployment failure
2. **Manual rollback:**
```bash
./deploy/scripts/deploy.sh production rollback previous-tag
```

## Security

### Container Security
- Non-root user execution
- Minimal base images
- Health checks for availability
- Resource limits and constraints

### Secrets Management
Sensitive configuration is managed through:
- GitHub repository secrets for CI/CD
- Environment variables for runtime
- Separate database credentials per environment

### Required GitHub Secrets
```
DOCKER_REGISTRY_USERNAME      # Container registry username
DOCKER_REGISTRY_PASSWORD      # Container registry password/token
STAGING_DEPLOY_SSH_KEY       # SSH key for staging deployment
STAGING_DEPLOY_HOST          # Staging server hostname
PRODUCTION_DEPLOY_SSH_KEY    # SSH key for production deployment
PRODUCTION_DEPLOY_HOST       # Production server hostname
```

## Integration

### CI/CD Integration

The deployment pipeline integrates with existing repository workflows:
- Extends the current `ci.yml` workflow
- Respects existing branch protection rules
- Maintains current testing and quality gates

### Development Workflow

1. Feature development on feature branches
2. Pull request with CI validation
3. Merge to `develop` triggers staging deployment
4. Merge to `main` triggers production deployment
5. Monitoring and alerting provide feedback

## Support

For deployment issues or questions:
1. Check this documentation
2. Review workflow logs in GitHub Actions
3. Check container logs using provided debugging commands
4. Consult the repository's issue tracker