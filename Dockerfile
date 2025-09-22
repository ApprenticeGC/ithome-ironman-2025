# GameConsole Container-Native Deployment
# Multi-stage build for optimized container images

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file and project files
COPY dotnet/TestSolution.sln ./
COPY dotnet/GameConsole.Host/GameConsole.Host.csproj ./GameConsole.Host/
COPY dotnet/GameConsole.Engine.Core/GameConsole.Engine.Core.csproj ./GameConsole.Engine.Core/
COPY dotnet/GameConsole.Core.Registry/GameConsole.Core.Registry.csproj ./GameConsole.Core.Registry/
COPY dotnet/GameConsole.Core.Abstractions/GameConsole.Core.Abstractions.csproj ./GameConsole.Core.Abstractions/
COPY dotnet/GameConsole.Plugins.Core/GameConsole.Plugins.Core.csproj ./GameConsole.Plugins.Core/
COPY dotnet/GameConsole.Input.Core/GameConsole.Input.Core.csproj ./GameConsole.Input.Core/
COPY dotnet/GameConsole.Graphics.Core/GameConsole.Graphics.Core.csproj ./GameConsole.Graphics.Core/
COPY dotnet/GameConsole.Audio.Core/GameConsole.Audio.Core.csproj ./GameConsole.Audio.Core/
COPY dotnet/GameConsole.Input.Services/GameConsole.Input.Services.csproj ./GameConsole.Input.Services/
COPY dotnet/GameConsole.Graphics.Services/GameConsole.Graphics.Services.csproj ./GameConsole.Graphics.Services/
COPY dotnet/GameConsole.Plugins.Lifecycle/GameConsole.Plugins.Lifecycle.csproj ./GameConsole.Plugins.Lifecycle/

# Restore dependencies
RUN dotnet restore GameConsole.Host/GameConsole.Host.csproj

# Copy source code
COPY dotnet/ ./

# Build and publish the application
RUN dotnet publish GameConsole.Host/GameConsole.Host.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1001 gameconsole && \
    adduser --system --uid 1001 --gid 1001 --no-create-home gameconsole

# Copy published application
COPY --from=build /app/out .

# Set ownership and permissions
RUN chown -R gameconsole:gameconsole /app
USER gameconsole

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD dotnet GameConsole.Host.dll --health-check || exit 1

# Expose port (if needed for future web components)
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "GameConsole.Host.dll"]