# GameConsole Deployment Guide
## RFC-012-02: Deployment Pipeline Automation

This document describes the container-native deployment pipeline for GameConsole, following RFC-012 Container-Native Deployment architecture.

## Overview

The deployment pipeline provides:
- **Automated CI/CD** via GitHub Actions
- **Multi-stage builds** with build, test, and runtime phases
- **Environment-specific configurations** for staging and production
- **Container-native deployment** with Docker and orchestration support
- **Health checks and monitoring** integration
- **Rollback capabilities** and deployment verification

## Architecture

### Build Pipeline
1. **Build & Test**: Compile .NET code, run unit tests
2. **Containerize**: Create Docker image, push to registry
3. **Deploy**: Deploy to target environment with configuration
4. **Verify**: Run health checks and deployment verification

### Container Structure
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:8.0`
- **Multi-stage Build**: SDK for build, runtime for deployment
- **Security**: Non-root user, minimal attack surface
- **Health Checks**: Built-in health monitoring endpoints

## Local Development

### Quick Start
```bash
# Build and run with Docker Compose
./scripts/dev.sh build
./scripts/dev.sh run

# Access application
open http://localhost:8080

# View logs
./scripts/dev.sh logs

# Stop and clean up
./scripts/dev.sh stop
```

### Manual Docker Commands
```bash
# Build image
docker build -t gameconsole:latest .

# Run with compose
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f gameconsole

# Stop
docker-compose down
```

## Deployment Environments

### Staging Environment
- **Purpose**: Integration testing, pre-production validation
- **Trigger**: Automatic on push to `main` branch
- **Configuration**: Single replica, reduced resources
- **URL**: `https://gameconsole.staging.example.com`

### Production Environment
- **Purpose**: Live application serving users
- **Trigger**: Manual workflow dispatch or tagged releases
- **Configuration**: Multi-replica, full resources, auto-scaling
- **URL**: `https://gameconsole.prod.example.com`

## Deployment Methods

### Automated Deployment (GitHub Actions)
The primary deployment method uses the `.github/workflows/deploy.yml` workflow:

```bash
# Deploy to staging (automatic on main branch)
git push origin main

# Deploy to production (manual trigger)
gh workflow run deploy --field environment=production --field force_deploy=false
```

### Manual Deployment
Use the deployment script for local or custom deployments:

```bash
# Deploy to staging
./scripts/deploy.sh -e staging -v latest

# Deploy to production with specific version
./scripts/deploy.sh -e production -v v1.2.3

# Dry run to see what would be deployed
./scripts/deploy.sh -e staging -v latest --dry-run
```

## Configuration Management

### Environment Variables
Applications can be configured via environment variables:
- `ASPNETCORE_ENVIRONMENT`: Set deployment environment
- `ASPNETCORE_URLS`: Configure listening URLs
- Custom application settings as needed

### Configuration Files
- `deployment-config.yml`: Environment-specific deployment settings
- `docker-compose.yml`: Local development orchestration
- `Dockerfile`: Container build configuration

## Health Checks and Monitoring

### Application Health
- **Endpoint**: `/health`
- **Port**: 8080
- **Checks**: Application startup, dependencies, resources

### Container Health
- **Docker Health Check**: Built into container
- **Kubernetes Probes**: Liveness and readiness probes
- **Monitoring**: Application metrics and logging

## Troubleshooting

### Common Issues

#### Build Failures
- Check .NET SDK version compatibility
- Verify all project references are correct
- Review build logs for specific errors

#### Container Issues
- Ensure Docker daemon is running
- Check container logs: `docker-compose logs gameconsole`
- Verify port availability (8080)

#### Deployment Failures
- Check deployment logs in GitHub Actions
- Verify container registry access
- Review environment-specific configuration

### Debugging Commands
```bash
# Check container status
docker-compose ps

# Access container shell
./scripts/dev.sh shell

# View detailed logs
docker-compose logs --details gameconsole

# Inspect image
docker inspect gameconsole:latest

# Test health endpoint
curl http://localhost:8080/health
```

## Security Considerations

### Container Security
- Non-root user execution
- Minimal base image attack surface
- No secrets in container images
- Regular security updates via base image updates

### Deployment Security
- Container registry access via GitHub tokens
- Environment-specific secrets management
- Network security and access controls
- Regular vulnerability scanning

## Rollback Procedures

### Automated Rollback
GitHub Actions deployment retains previous versions for easy rollback:

```bash
# Rollback to previous version
./scripts/deploy.sh -e production -v previous-version
```

### Manual Rollback
In case of emergency, rollback can be performed manually:

```bash
# Deploy specific known-good version
./scripts/deploy.sh -e production -v v1.1.0

# Or use container registry directly
docker pull ghcr.io/apprenticegc/ithome-ironman-2025:v1.1.0
```

## Performance and Scaling

### Resource Requirements
- **Staging**: 128Mi memory, 100m CPU minimum
- **Production**: 256Mi memory, 250m CPU minimum
- **Scaling**: Horizontal pod autoscaling based on CPU utilization

### Performance Monitoring
- Application metrics via health endpoints
- Container resource monitoring
- Performance benchmarking in CI pipeline

## Future Enhancements

- **Blue-Green Deployments**: Zero-downtime deployments
- **Canary Releases**: Gradual rollout with monitoring
- **Multi-Region Deployment**: Geographic distribution
- **Advanced Monitoring**: APM and distributed tracing
- **Automated Testing**: Integration and load testing in pipeline

---

For questions or issues with deployment, refer to the [troubleshooting section](#troubleshooting) or check the GitHub Actions workflow logs.