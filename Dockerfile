# GameConsole Engine Container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files
COPY ["dotnet/TestSolution.sln", "dotnet/"]
COPY ["dotnet/GameConsole.Engine.Core/GameConsole.Engine.Core.csproj", "dotnet/GameConsole.Engine.Core/"]
COPY ["dotnet/GameConsole.Core.Abstractions/GameConsole.Core.Abstractions.csproj", "dotnet/GameConsole.Core.Abstractions/"]
COPY ["dotnet/GameConsole.Core.Registry/GameConsole.Core.Registry.csproj", "dotnet/GameConsole.Core.Registry/"]
COPY ["dotnet/GameConsole.Audio.Core/GameConsole.Audio.Core.csproj", "dotnet/GameConsole.Audio.Core/"]
COPY ["dotnet/GameConsole.Graphics.Core/GameConsole.Graphics.Core.csproj", "dotnet/GameConsole.Graphics.Core/"]
COPY ["dotnet/GameConsole.Graphics.Services/GameConsole.Graphics.Services.csproj", "dotnet/GameConsole.Graphics.Services/"]
COPY ["dotnet/GameConsole.Input.Core/GameConsole.Input.Core.csproj", "dotnet/GameConsole.Input.Core/"]
COPY ["dotnet/GameConsole.Input.Services/GameConsole.Input.Services.csproj", "dotnet/GameConsole.Input.Services/"]
COPY ["dotnet/GameConsole.Plugins.Core/GameConsole.Plugins.Core.csproj", "dotnet/GameConsole.Plugins.Core/"]
COPY ["dotnet/GameConsole.Plugins.Lifecycle/GameConsole.Plugins.Lifecycle.csproj", "dotnet/GameConsole.Plugins.Lifecycle/"]

# Restore packages
RUN dotnet restore "dotnet/TestSolution.sln"

# Copy all source files
COPY dotnet/ dotnet/

# Build the application
WORKDIR "/src/dotnet"
RUN dotnet build "TestSolution.sln" -c $BUILD_CONFIGURATION --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GameConsole.Engine.Core/GameConsole.Engine.Core.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a simple entrypoint for the containerized engine
RUN echo '#!/bin/bash\necho "GameConsole Engine Container Started"\necho "Available assemblies:"\nls -la *.dll\necho "Container ready - use docker exec to interact"\nexec tail -f /dev/null' > entrypoint.sh && chmod +x entrypoint.sh

ENTRYPOINT ["./entrypoint.sh"]