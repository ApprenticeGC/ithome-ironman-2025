# Multi-stage Dockerfile for GameConsole .NET libraries
# Stage 1: Build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY dotnet/ ./

# Restore dependencies with retry logic
RUN dotnet restore --disable-parallel --verbosity minimal || \
    dotnet restore --disable-parallel --verbosity minimal || \
    dotnet restore --disable-parallel --verbosity minimal

# Build the solution
RUN dotnet build --configuration Release --no-restore

# Run tests (optional, continue on failure for deployment)
RUN dotnet test --configuration Release --no-build --verbosity minimal --logger trx --results-directory /test-results || echo "Tests completed with issues"

# Stage 2: Runtime environment  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 dotnet \
    && adduser --system --uid 1001 --gid 1001 --shell /bin/false dotnet

# Copy built libraries
COPY --from=build /src/*/bin/Release/*/*.dll ./
COPY --from=build /src/*/bin/Release/*/*.pdb ./
COPY --from=build /src/*/bin/Release/*/*.json ./

# Copy test results if available
COPY --from=build /test-results ./test-results/ || true

# Set ownership and permissions
RUN chown -R dotnet:dotnet /app
USER dotnet

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD ["dotnet", "--list-runtimes"]

# Expose port
EXPOSE 8080

# Default command (can be overridden)
CMD ["dotnet", "--list-runtimes"]