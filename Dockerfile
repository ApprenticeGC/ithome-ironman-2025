# GameConsole Deployment Container
# Multi-stage build for optimized production container

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY dotnet/ ./dotnet/
RUN dotnet restore ./dotnet/TestSolution.sln

# Build the solution
RUN dotnet build ./dotnet/TestSolution.sln -c Release --no-restore

# Test stage (optional - can be skipped in production builds)
FROM build AS test
RUN dotnet test ./dotnet/TestSolution.sln -c Release --no-build --verbosity normal

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' gameuser && \
    chown -R gameuser:gameuser /app
USER gameuser

# Copy built artifacts from build stage
COPY --from=build --chown=gameuser:gameuser /src/dotnet/*/bin/Release/net8.0/ ./

# Health check endpoint (placeholder for future implementation)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD echo "GameConsole container is healthy"

# Expose default port (configurable via environment)
EXPOSE 8080

# Default entry point - can be overridden for specific services
CMD ["echo", "GameConsole runtime container ready. Override CMD to run specific services."]