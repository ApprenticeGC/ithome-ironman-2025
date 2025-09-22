# GameConsole Deployment Guide

This guide covers the container-native deployment automation for the GameConsole project, implementing RFC-012-02.

## Overview

The GameConsole project uses Docker containers and automated CI/CD pipelines for deployment. The deployment pipeline supports:

- Multi-stage Docker builds for optimized container images
- Automated testing and building via GitHub Actions
- Container registry publishing (GitHub Container Registry)
- Environment-specific deployments (staging, production)
- Docker Compose orchestration for local development

## Quick Start

### Local Development

1. **Start the application locally with Docker Compose:**
   ```bash
   docker-compose up --build
   ```

2. **Access the application:**
   - GameConsole: `http://localhost:8080`
   - With reverse proxy: `http://localhost:80`

3. **View logs:**
   ```bash
   docker-compose logs -f gameconsole
   ```

### Production Deployment

1. **Deploy with production profile:**
   ```bash
   docker-compose --profile production up -d
   ```

2. **Deploy specific version:**
   ```bash
   IMAGE_TAG=v1.0.0 docker-compose up -d
   ```

## Architecture

### Container Structure

- **Base Image**: mcr.microsoft.com/dotnet/aspnet:8.0
- **Build Image**: mcr.microsoft.com/dotnet/sdk:8.0
- **Multi-stage build**: Optimizes final image size
- **Health checks**: Built-in container health monitoring

### Services

- **gameconsole**: Main application container
- **nginx** (optional): Reverse proxy for production
- **volumes**: Persistent configuration and logs

## CI/CD Pipeline

The deployment pipeline (`.github/workflows/deploy.yml`) includes:

### Triggers
- Push to `main` branch
- Git tags starting with `v*`
- Pull requests
- Manual workflow dispatch

### Jobs

1. **build-and-test**: Builds .NET solution and runs tests
2. **build-container**: Creates and publishes container images
3. **deploy-staging**: Deploys to staging environment (main branch)
4. **deploy-production**: Deploys to production (tags only)

### Container Registry

Images are published to GitHub Container Registry:
- Repository: `ghcr.io/{owner}/{repo}/gameconsole`
- Tags: branch names, PR numbers, semantic versions, latest

## Configuration

### Environment Variables

- `DOTNET_ENVIRONMENT`: Development/Staging/Production
- `ASPNETCORE_URLS`: Listening URLs (default: http://+:8080)

### Configuration Files

- `deployment/config/appsettings.json`: Base configuration
- `deployment/config/appsettings.Production.json`: Production overrides
- `deployment/nginx/nginx.conf`: Reverse proxy configuration

### Docker Compose Profiles

- **default**: Basic gameconsole service
- **production**: Includes nginx reverse proxy

## Deployment Commands

### Build and Test Locally
```bash
# Build the Docker image
docker build -t gameconsole:local .

# Run tests
docker run --rm gameconsole:local dotnet test --configuration Release
```

### Manual Deployment
```bash
# Deploy to staging
docker-compose -f deployment/manifests/staging.yml up -d

# Deploy to production
docker-compose -f deployment/manifests/production.yml up -d

# Scale production deployment
docker-compose -f deployment/manifests/production.yml up -d --scale gameconsole=3
```

### Monitoring and Logs
```bash
# View service status
docker-compose ps

# Follow logs
docker-compose logs -f

# Health check
curl http://localhost:8080/health
```

## Security Considerations

### Container Security
- Non-root user execution
- Minimal base image (aspnet runtime)
- No sensitive data in images

### Network Security
- SSL/TLS termination at reverse proxy
- Internal service communication
- Health check endpoints

### Secrets Management
- Use environment variables for secrets
- Mount sensitive files as volumes
- Leverage orchestration platform secret management

## Troubleshooting

### Common Issues

1. **Build Failures**
   - Check .NET SDK version compatibility
   - Verify all project references resolve correctly
   - Ensure NuGet package restoration succeeds

2. **Container Startup Issues**
   - Verify environment variables are set correctly
   - Check port availability (8080)
   - Review application logs

3. **Deployment Failures**
   - Confirm container registry authentication
   - Validate deployment manifest syntax
   - Check resource quotas and limits

### Debugging Commands

```bash
# Debug container build
docker build --no-cache --progress=plain -t gameconsole:debug .

# Run container interactively
docker run -it --entrypoint /bin/bash gameconsole:debug

# Inspect container
docker inspect gameconsole:local

# View container logs
docker logs <container_id>
```

## Performance Tuning

### Resource Limits
- Memory: 256MB-512MB recommended
- CPU: 0.5-1.0 cores for typical workloads
- Adjust based on actual usage patterns

### Scaling
- Horizontal scaling supported via container orchestration
- Load balancing handled by reverse proxy
- Session state should be stateless or externalized

## Monitoring

### Health Checks
- Built-in Docker health checks
- Application-level health endpoints
- External monitoring integration points

### Observability
- Structured logging to stdout
- Metrics collection endpoints
- Distributed tracing support

## Next Steps

1. **Implement application-specific health checks**
2. **Add metrics and monitoring endpoints**
3. **Integrate with container orchestration platforms**
4. **Implement automated database migrations**
5. **Add comprehensive smoke tests**

## References

- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)