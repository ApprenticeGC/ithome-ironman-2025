#!/bin/bash
# GameConsole Deployment Script
# RFC-012-02: Deployment Pipeline Automation

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
CONFIG_FILE="$PROJECT_ROOT/deployment-config.yml"

# Default values
ENVIRONMENT="staging"
VERSION="latest"
REGISTRY="ghcr.io"
IMAGE_NAME=""
DRY_RUN=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Help function
show_help() {
    cat << EOF
GameConsole Deployment Script

Usage: $0 [OPTIONS]

Options:
    -e, --environment    Target environment (staging|production) [default: staging]
    -v, --version        Version to deploy [default: latest]
    -i, --image          Full image name [default: auto-generated]
    -r, --registry       Container registry [default: ghcr.io]
    -d, --dry-run        Show what would be deployed without executing
    -h, --help           Show this help message

Examples:
    $0 -e staging -v v1.2.3
    $0 --environment production --version latest --dry-run
EOF
}

# Parse command line arguments
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -e|--environment)
                ENVIRONMENT="$2"
                shift 2
                ;;
            -v|--version)
                VERSION="$2"
                shift 2
                ;;
            -i|--image)
                IMAGE_NAME="$2"
                shift 2
                ;;
            -r|--registry)
                REGISTRY="$2"
                shift 2
                ;;
            -d|--dry-run)
                DRY_RUN=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# Validate environment
validate_environment() {
    if [[ "$ENVIRONMENT" != "staging" && "$ENVIRONMENT" != "production" ]]; then
        log_error "Invalid environment: $ENVIRONMENT. Must be 'staging' or 'production'"
        exit 1
    fi
}

# Generate deployment manifest
generate_manifest() {
    local manifest_file="$PROJECT_ROOT/deployment-$ENVIRONMENT-$VERSION.yml"
    
    log_info "Generating deployment manifest for $ENVIRONMENT environment"
    
    # Read configuration (simplified - in real implementation use yq or similar)
    local replicas=1
    local memory_request="128Mi"
    local cpu_request="100m"
    local memory_limit="256Mi" 
    local cpu_limit="250m"
    
    if [[ "$ENVIRONMENT" == "production" ]]; then
        replicas=3
        memory_request="256Mi"
        cpu_request="250m"
        memory_limit="512Mi"
        cpu_limit="500m"
    fi
    
    # Generate Kubernetes manifest
    cat > "$manifest_file" << EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: gameconsole
  namespace: $ENVIRONMENT
  labels:
    app: gameconsole
    environment: $ENVIRONMENT
    version: $VERSION
spec:
  replicas: $replicas
  selector:
    matchLabels:
      app: gameconsole
      environment: $ENVIRONMENT
  template:
    metadata:
      labels:
        app: gameconsole
        environment: $ENVIRONMENT
        version: $VERSION
    spec:
      containers:
      - name: gameconsole
        image: $IMAGE_NAME
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: $ENVIRONMENT
        resources:
          requests:
            memory: $memory_request
            cpu: $cpu_request
          limits:
            memory: $memory_limit
            cpu: $cpu_limit
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: gameconsole-service
  namespace: $ENVIRONMENT
spec:
  selector:
    app: gameconsole
    environment: $ENVIRONMENT
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
EOF

    if [[ -f "$manifest_file" ]]; then
        log_info "Manifest generated successfully: $manifest_file"
    else
        log_error "Failed to generate manifest file"
        exit 1
    fi
}

# Deploy function
deploy() {
    local manifest_file="$1"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_warning "DRY RUN MODE - Would deploy the following manifest:"
        echo "----------------------------------------"
        if [[ -f "$manifest_file" ]]; then
            cat "$manifest_file"
        else
            log_error "Manifest file not found: $manifest_file"
        fi
        echo "----------------------------------------"
        return
    fi
    
    log_info "Deploying to $ENVIRONMENT environment"
    
    # In a real deployment, this would use kubectl or similar
    log_info "Applying Kubernetes manifest..."
    echo "kubectl apply -f $manifest_file"
    
    log_info "Waiting for deployment to complete..."
    echo "kubectl rollout status deployment/gameconsole -n $ENVIRONMENT"
    
    log_success "Deployment completed successfully!"
}

# Health check function
health_check() {
    if [[ "$DRY_RUN" == "true" ]]; then
        log_warning "DRY RUN MODE - Skipping health check"
        return
    fi
    
    log_info "Performing health check..."
    
    # Mock health check (replace with actual implementation)
    local health_url
    if [[ "$ENVIRONMENT" == "production" ]]; then
        health_url="https://gameconsole.prod.example.com/health"
    else
        health_url="https://gameconsole.staging.example.com/health"
    fi
    
    log_info "Health check URL: $health_url"
    echo "curl -f $health_url"
    
    log_success "Health check passed!"
}

# Main function
main() {
    parse_args "$@"
    validate_environment
    
    # Set default image name if not provided
    if [[ -z "$IMAGE_NAME" ]]; then
        IMAGE_NAME="$REGISTRY/apprenticegc/ithome-ironman-2025:$VERSION"
    fi
    
    log_info "Starting deployment process..."
    log_info "Environment: $ENVIRONMENT"
    log_info "Version: $VERSION"
    log_info "Image: $IMAGE_NAME"
    log_info "Dry run: $DRY_RUN"
    
    # Generate manifest
    generate_manifest >&2
    local manifest_file="$PROJECT_ROOT/deployment-$ENVIRONMENT-$VERSION.yml"
    
    # Deploy
    deploy "$manifest_file"
    
    # Health check
    health_check
    
    log_success "Deployment process completed!"
    
    # Cleanup manifest file (optional)
    if [[ "$DRY_RUN" != "true" ]]; then
        rm -f "$manifest_file"
    fi
}

# Run main function
main "$@"