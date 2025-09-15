# RFC-012: Container-Native Deployment

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-001, RFC-007

## Summary

This RFC defines the container-native deployment strategy for GameConsole, supporting both local development and production scaling. The system uses Docker Compose for development environments and provides Kubernetes manifests for production deployment with AI gateway services.

## Motivation

GameConsole needs flexible deployment options that support:

1. **Local Development**: Easy setup with Docker Compose and local Ollama
2. **Production Scaling**: Kubernetes deployment with AI gateway services
3. **Hybrid AI Deployment**: Mix local and remote AI services
4. **Service Mesh Ready**: Clear service boundaries for advanced networking
5. **Development Parity**: Consistent behavior across dev/staging/production environments
6. **Resource Efficiency**: Optimal resource utilization for different workloads

Container-native deployment enables consistent environments and simplifies complex multi-service orchestration.

## Detailed Design

### Container Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    GameConsole Deployment                    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │  GameConsole    │  │   AI Gateway    │  │   Ollama    │ │
│  │     Host        │  │   (Akka.NET)    │  │   Server    │ │
│  │                 │  │                 │  │             │ │
│  │ • CLI/TUI       │  │ • LLM Workers   │  │ • Models    │ │
│  │ • Plugin System │  │ • Tool Bus      │  │ • GPU Accel │ │
│  │ • Game/Editor   │  │ • Rate Limiting │  │ • Health    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
│           │                      │                    │     │
│           └──────────────────────┼────────────────────┘     │
│                                  │                          │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              Shared Volume Storage                      │ │
│  │  • Projects     • Plugins     • Models    • Logs      │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Development Environment (Docker Compose)

#### Development Stack Configuration
```yaml
# docker-compose.dev.yml
version: "3.9"

services:
  ollama:
    image: ollama/ollama:latest
    container_name: gameconsole-ollama-dev
    restart: unless-stopped
    ports:
      - "11434:11434"
    volumes:
      - ollama-models:/root/.ollama
      - ./ollama/entrypoint.sh:/entrypoint.sh:ro
    environment:
      - OLLAMA_HOST=0.0.0.0
      - OLLAMA_ORIGINS=*
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:11434/api/tags"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    # GPU support for faster local inference
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]

  gameconsole:
    build:
      context: .
      dockerfile: Dockerfile.dev
      target: development
    container_name: gameconsole-dev
    restart: unless-stopped
    depends_on:
      ollama:
        condition: service_healthy
    environment:
      # Application configuration
      - GAMECONSOLE_MODE=Editor
      - GAMECONSOLE_UI_MODE=tui
      - ASPNETCORE_ENVIRONMENT=Development

      # AI configuration
      - AI_PROFILE=EditorAuthoring
      - AI_GATEWAY_MODE=Local
      - OLLAMA_BASE_URL=http://ollama:11434/v1
      - OLLAMA_MODEL=llama3.1:8b

      # Plugin configuration
      - PLUGINS_DIRECTORY=/app/plugins
      - PLUGINS_HOT_RELOAD=true

      # Logging
      - SERILOG__MINIMUMLEVEL__DEFAULT=Debug
      - SERILOG__WRITETO__0__NAME=Console
    volumes:
      - ./projects:/app/projects          # Project files
      - ./plugins:/app/plugins           # Plugin assemblies
      - ./logs:/app/logs                 # Log files
      - ./config:/app/config             # Configuration overrides
    ports:
      - "5000:5000"                      # HTTP API (if enabled)
      - "5001:5001"                      # gRPC API (if enabled)
    stdin_open: true                     # Enable interactive mode
    tty: true                           # Allocate TTY for TUI
    networks:
      - gameconsole-dev

  # Optional: Redis for caching and state
  redis:
    image: redis:7-alpine
    container_name: gameconsole-redis-dev
    restart: unless-stopped
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    networks:
      - gameconsole-dev

networks:
  gameconsole-dev:
    driver: bridge

volumes:
  ollama-models:
    driver: local
  redis-data:
    driver: local
```

#### Development Dockerfile
```dockerfile
# Dockerfile.dev
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app

# Install development tools
RUN apt-get update && apt-get install -y \
    curl \
    git \
    vim \
    htop \
    && rm -rf /var/lib/apt/lists/*

# Development target - includes source and can hot reload
FROM base AS development
WORKDIR /src

# Copy solution file and project files for dependency restoration
COPY ["GameConsole.sln", "./"]
COPY ["src/", "src/"]
COPY ["providers/", "providers/"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "GameConsole.sln"

# Set up development environment
WORKDIR /app
COPY . .

# Build in development configuration
RUN dotnet build "GameConsole.sln" -c Debug --no-restore

# Set entrypoint for development
ENTRYPOINT ["dotnet", "run", "--project", "src/host/GameConsole.Host/GameConsole.Host.csproj", "--configuration", "Debug"]

# Production target - optimized build
FROM base AS production
WORKDIR /app

# Copy published application
COPY --from=development /app/src/host/GameConsole.Host/bin/Debug/net8.0/publish .

# Create required directories
RUN mkdir -p plugins projects logs config

# Set entrypoint for production
ENTRYPOINT ["dotnet", "GameConsole.Host.dll"]
```

### Production Environment (Kubernetes)

#### Production Stack with AI Gateway
```yaml
# docker-compose.prod.yml
version: "3.9"

services:
  ollama:
    image: ollama/ollama:latest
    container_name: gameconsole-ollama-prod
    restart: unless-stopped
    ports:
      - "11434:11434"
    volumes:
      - ollama-models:/root/.ollama
      - ./ollama/models.txt:/models.txt:ro
    environment:
      - OLLAMA_HOST=0.0.0.0
      - OLLAMA_ORIGINS=*
    deploy:
      resources:
        limits:
          memory: 8G
        reservations:
          devices:
            - driver: nvidia
              count: 1
              capabilities: [gpu]
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:11434/api/tags"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - gameconsole-backend

  ai-gateway:
    build:
      context: ./ai-gateway
      dockerfile: Dockerfile
    container_name: gameconsole-ai-gateway
    restart: unless-stopped
    depends_on:
      - ollama
    environment:
      # Akka.NET configuration
      - AKKA__ACTOR__PROVIDER=remote
      - AKKA__REMOTE__DOT-NETTY__TCP__HOSTNAME=0.0.0.0
      - AKKA__REMOTE__DOT-NETTY__TCP__PORT=4053

      # AI gateway configuration
      - OLLAMA_BASE_URL=http://ollama:11434
      - GATEWAY_WORKERS=4
      - MAX_TOKENS_PER_REQUEST=512
      - RATE_LIMIT_REQUESTS_PER_MINUTE=300

      # Monitoring
      - PROMETHEUS_METRICS_PORT=9090
    ports:
      - "4053:4053"                      # Akka.Remote TCP
      - "8080:8080"                      # HTTP/gRPC API
      - "9090:9090"                      # Prometheus metrics
    volumes:
      - ./logs:/app/logs
    networks:
      - gameconsole-backend
      - gameconsole-frontend

  gameconsole:
    build:
      context: .
      dockerfile: Dockerfile
      target: production
    container_name: gameconsole-prod
    restart: unless-stopped
    depends_on:
      - ai-gateway
    environment:
      # Application configuration
      - GAMECONSOLE_MODE=Game
      - GAMECONSOLE_UI_MODE=cli
      - ASPNETCORE_ENVIRONMENT=Production

      # AI configuration
      - AI_PROFILE=RuntimeDirector
      - AI_GATEWAY_MODE=Remote
      - AI_GATEWAY_HOST=ai-gateway
      - AI_GATEWAY_PORT=4053

      # Performance tuning
      - DOTNET_GCServer=1
      - DOTNET_gcConcurrent=1
      - DOTNET_ThreadPool_ForceMaxWorkerThreads=100
    volumes:
      - ./projects:/app/projects:ro       # Read-only project files
      - ./plugins:/app/plugins:ro         # Read-only plugins
      - ./logs:/app/logs                  # Log files
    ports:
      - "5000:5000"                      # HTTP API
    networks:
      - gameconsole-frontend
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: "1.0"
        reservations:
          memory: 512M
          cpus: "0.5"

  # Production monitoring
  prometheus:
    image: prom/prometheus:latest
    container_name: gameconsole-prometheus
    restart: unless-stopped
    ports:
      - "9091:9090"
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
    networks:
      - gameconsole-backend

networks:
  gameconsole-frontend:
    driver: bridge
  gameconsole-backend:
    driver: bridge
    internal: true  # Backend services not exposed externally

volumes:
  ollama-models:
    driver: local
  prometheus-data:
    driver: local
```

### Kubernetes Deployment

#### GameConsole Deployment Manifest
```yaml
# k8s/gameconsole-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gameconsole
  namespace: gameconsole
  labels:
    app: gameconsole
    version: v1.0.0
spec:
  replicas: 2
  selector:
    matchLabels:
      app: gameconsole
  template:
    metadata:
      labels:
        app: gameconsole
        version: v1.0.0
    spec:
      containers:
      - name: gameconsole
        image: gameconsole:latest
        ports:
        - containerPort: 5000
          name: http
        - containerPort: 5001
          name: grpc
        env:
        - name: GAMECONSOLE_MODE
          value: "Game"
        - name: AI_GATEWAY_HOST
          value: "ai-gateway-service"
        - name: AI_GATEWAY_PORT
          value: "4053"
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        volumeMounts:
        - name: projects-storage
          mountPath: /app/projects
          readOnly: true
        - name: plugins-storage
          mountPath: /app/plugins
          readOnly: true
        - name: logs-storage
          mountPath: /app/logs
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
      volumes:
      - name: projects-storage
        persistentVolumeClaim:
          claimName: projects-pvc
      - name: plugins-storage
        configMap:
          name: plugins-config
      - name: logs-storage
        emptyDir: {}

---
apiVersion: v1
kind: Service
metadata:
  name: gameconsole-service
  namespace: gameconsole
spec:
  selector:
    app: gameconsole
  ports:
  - name: http
    port: 80
    targetPort: 5000
  - name: grpc
    port: 5001
    targetPort: 5001
  type: ClusterIP
```

#### AI Gateway Deployment
```yaml
# k8s/ai-gateway-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ai-gateway
  namespace: gameconsole
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ai-gateway
  template:
    metadata:
      labels:
        app: ai-gateway
    spec:
      containers:
      - name: ai-gateway
        image: gameconsole-ai-gateway:latest
        ports:
        - containerPort: 4053
          name: akka-remote
        - containerPort: 8080
          name: http
        - containerPort: 9090
          name: metrics
        env:
        - name: AKKA__REMOTE__DOT-NETTY__TCP__HOSTNAME
          valueFrom:
            fieldRef:
              fieldPath: status.podIP
        - name: OLLAMA_BASE_URL
          value: "http://ollama-service:11434"
        - name: GATEWAY_WORKERS
          value: "4"
        resources:
          requests:
            memory: "1Gi"
            cpu: "500m"
          limits:
            memory: "4Gi"
            cpu: "2000m"

---
apiVersion: v1
kind: Service
metadata:
  name: ai-gateway-service
  namespace: gameconsole
spec:
  selector:
    app: ai-gateway
  ports:
  - name: akka-remote
    port: 4053
    targetPort: 4053
  - name: http
    port: 8080
    targetPort: 8080
  type: ClusterIP

---
# Headless service for Akka cluster discovery
apiVersion: v1
kind: Service
metadata:
  name: ai-gateway-headless
  namespace: gameconsole
spec:
  clusterIP: None
  selector:
    app: ai-gateway
  ports:
  - name: akka-remote
    port: 4053
    targetPort: 4053
```

#### Ollama Deployment
```yaml
# k8s/ollama-deployment.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: ollama
  namespace: gameconsole
spec:
  serviceName: ollama
  replicas: 1  # Single replica due to GPU constraints
  selector:
    matchLabels:
      app: ollama
  template:
    metadata:
      labels:
        app: ollama
    spec:
      nodeSelector:
        gpu: nvidia  # Schedule on GPU nodes
      containers:
      - name: ollama
        image: ollama/ollama:latest
        ports:
        - containerPort: 11434
          name: http
        env:
        - name: OLLAMA_HOST
          value: "0.0.0.0"
        resources:
          requests:
            memory: "4Gi"
            cpu: "2000m"
            nvidia.com/gpu: 1
          limits:
            memory: "8Gi"
            cpu: "4000m"
            nvidia.com/gpu: 1
        volumeMounts:
        - name: ollama-data
          mountPath: /root/.ollama
        livenessProbe:
          httpGet:
            path: /api/tags
            port: 11434
          initialDelaySeconds: 60
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /api/tags
            port: 11434
          initialDelaySeconds: 30
          periodSeconds: 10
  volumeClaimTemplates:
  - metadata:
      name: ollama-data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 50Gi

---
apiVersion: v1
kind: Service
metadata:
  name: ollama-service
  namespace: gameconsole
spec:
  selector:
    app: ollama
  ports:
  - name: http
    port: 11434
    targetPort: 11434
  type: ClusterIP
```

### Environment Configuration Management

#### Configuration via ConfigMaps and Secrets
```yaml
# k8s/gameconsole-config.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: gameconsole-config
  namespace: gameconsole
data:
  appsettings.Production.json: |
    {
      "GameConsole": {
        "Mode": "Game",
        "UIMode": "cli",
        "PluginsDirectory": "/app/plugins",
        "HotReload": false
      },
      "AI": {
        "Profile": "RuntimeDirector",
        "GatewayMode": "Remote",
        "GatewayHost": "ai-gateway-service",
        "GatewayPort": 4053,
        "Timeout": "00:00:03"
      },
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning"
          }
        }
      }
    }

---
apiVersion: v1
kind: Secret
metadata:
  name: gameconsole-secrets
  namespace: gameconsole
type: Opaque
data:
  # Base64 encoded secrets
  openai-api-key: ""  # If using OpenAI fallback
  azure-api-key: ""   # If using Azure fallback
```

### Monitoring and Observability

#### Prometheus Configuration
```yaml
# monitoring/prometheus.yml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  - "gameconsole-rules.yml"

scrape_configs:
  - job_name: 'gameconsole'
    static_configs:
      - targets: ['gameconsole:5000']
    metrics_path: '/metrics'

  - job_name: 'ai-gateway'
    static_configs:
      - targets: ['ai-gateway:9090']

  - job_name: 'ollama'
    static_configs:
      - targets: ['ollama:11434']
    metrics_path: '/metrics'

alerting:
  alertmanagers:
    - static_configs:
        - targets: ['alertmanager:9093']
```

#### Health Check Endpoints
```csharp
// GameConsole.Host/HealthChecks/GameConsoleHealthCheck.cs
public class GameConsoleHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var checks = new Dictionary<string, object>();

        // Check plugin system
        try
        {
            var pluginService = _serviceProvider.GetRequiredService<GameConsole.Plugin.Services.IService>();
            var loadedPlugins = pluginService.GetLoadedPlugins().Count();
            checks["loaded_plugins"] = loadedPlugins;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Plugin system unhealthy", ex);
        }

        // Check AI system
        try
        {
            var aiService = _serviceProvider.GetRequiredService<GameConsole.AI.Services.IService>();
            var agents = aiService.GetAvailableAgents().Count();
            checks["available_agents"] = agents;
        }
        catch (Exception ex)
        {
            checks["ai_system_error"] = ex.Message;
        }

        return HealthCheckResult.Healthy("GameConsole is healthy", checks);
    }
}
```

## Benefits

### Development Experience
- **Fast Setup**: `docker-compose up` gets full environment running
- **Hot Reload**: Live plugin development without container rebuilds
- **Consistent Environment**: Same behavior across all developer machines
- **GPU Support**: Local AI acceleration for faster iteration

### Production Readiness
- **Scalability**: Kubernetes horizontal scaling for high load
- **Reliability**: Health checks, rolling updates, service mesh ready
- **Monitoring**: Prometheus metrics and alerting integration
- **Resource Efficiency**: Optimized resource limits and requests

### Operational Benefits
- **Infrastructure as Code**: All deployment configs versioned
- **Service Isolation**: Clear boundaries between GameConsole, AI, and storage
- **Backup/Restore**: Persistent volumes for stateful data
- **Security**: Network policies and secret management

## Drawbacks

### Complexity
- **Multi-Container Orchestration**: More moving parts to manage
- **Network Configuration**: Service discovery and connectivity
- **Resource Requirements**: GPU nodes, persistent storage, monitoring stack

### Development Overhead
- **Docker Knowledge**: Developers need container expertise
- **Local Resources**: GPU support requires specific hardware
- **Debug Complexity**: Debugging across container boundaries

## Alternatives Considered

### Serverless Deployment
- **Considered**: AWS Lambda/Azure Functions for GameConsole host
- **Rejected**: Long-running processes, plugin loading, and AI integration don't fit serverless model

### VM-Based Deployment
- **Considered**: Traditional virtual machine deployment
- **Rejected**: Less efficient resource utilization, harder to scale, environment drift

### Single Container Deployment
- **Considered**: All services in one container
- **Rejected**: Monolithic approach prevents independent scaling and updates

## Implementation Roadmap

### Phase 1: Development Environment
- Docker Compose development stack
- Local Ollama integration
- Hot reload support
- Basic health checks

### Phase 2: Production Containers
- Production Dockerfile optimization
- AI Gateway containerization
- Kubernetes manifests
- Secret management

### Phase 3: Monitoring & Operations
- Prometheus metrics integration
- Grafana dashboards
- Alerting rules
- Log aggregation

### Phase 4: Advanced Features
- Service mesh integration (Istio/Linkerd)
- Multi-region deployment
- Advanced security policies
- Auto-scaling based on AI load

## Success Metrics

- **Development Setup Time**: < 5 minutes from clone to running
- **Production Deployment Time**: < 10 minutes for full stack
- **Resource Utilization**: > 80% CPU/memory efficiency in production
- **Service Availability**: > 99.9% uptime for core services

## Future Enhancements

- **Multi-Cloud Deployment**: Support AWS EKS, Azure AKS, Google GKE
- **Edge Deployment**: Run GameConsole closer to users/developers
- **Serverless AI**: On-demand AI service scaling
- **GitOps Integration**: Automated deployment via Git workflows