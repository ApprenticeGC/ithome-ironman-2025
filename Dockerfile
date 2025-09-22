# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY dotnet/ ./dotnet/
COPY build/ ./build/

# Restore dependencies
RUN dotnet restore ./dotnet

# Build the solution
RUN dotnet build ./dotnet --configuration Release --no-restore

# Test the solution
RUN dotnet test ./dotnet --configuration Release --no-build --verbosity normal

# Package stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Copy the built artifacts
COPY --from=build /src/dotnet/*/bin/Release/net*/ ./libs/

# Set up environment
ENV DOTNET_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Expose port
EXPOSE 8080

# The container will be used as a base for GameConsole applications
# Applications can add their own entry point by extending this image
CMD ["dotnet", "--info"]