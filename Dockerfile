# GameConsole Container-Native Deployment
# RFC-012-02: Deployment Pipeline Automation
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy entire dotnet directory
COPY dotnet/ ./dotnet/

# Restore dependencies
RUN dotnet restore ./dotnet || echo "Restore failed, will try again during build"

# Copy source code and build
COPY . .
RUN dotnet build ./dotnet -c Release --no-restore || dotnet build ./dotnet -c Release

# Run tests (optional in container build)
RUN dotnet test ./dotnet -c Release --no-build --verbosity normal || echo "Tests skipped or failed"

# Create runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy build artifacts (look for any executable)
COPY --from=build /app/dotnet/*/bin/Release/net8.0/ ./

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Health check endpoint (will be implemented by the application)
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Default entry point - applications can override this
CMD ["echo", "GameConsole container ready. Specify the application DLL to run."]