#!/bin/bash
# GameConsole Deployment Script
# This script handles deployment automation for GameConsole services

set -euo pipefail

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DEPLOY_CONFIG_DIR="${PROJECT_ROOT}/deploy/config"
DOCKER_COMPOSE_FILE="${PROJECT_ROOT}/docker-compose.yml"

# Default values
ENVIRONMENT="${1:-development}"
ACTION="${2:-deploy}"
IMAGE_TAG="${3:-latest}"
DRY_RUN="${DRY_RUN:-false}"

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

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    local missing_tools=()
    
    command -v docker >/dev/null 2>&1 || missing_tools+=("docker")
    command -v docker-compose >/dev/null 2>&1 || missing_tools+=("docker-compose")
    
    if [[ ${#missing_tools[@]} -ne 0 ]]; then
        log_error "Missing required tools: ${missing_tools[*]}"
        log_error "Please install the missing tools and try again."
        exit 1
    fi
}

# Load environment configuration
load_environment() {
    local env_file="${DEPLOY_CONFIG_DIR}/${ENVIRONMENT}.env"
    
    if [[ -f "$env_file" ]]; then
        log_info "Loading environment configuration from $env_file"
        source "$env_file"
    else
        log_warn "Environment file $env_file not found, using defaults"
    fi
    
    # Set deployment-specific variables
    export DEPLOY_ENVIRONMENT="$ENVIRONMENT"
    export DEPLOY_TAG="$IMAGE_TAG"
    export DEPLOY_TIMESTAMP="$(date -u +"%Y-%m-%dT%H:%M:%SZ")"
    export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-gameconsole-${ENVIRONMENT}}"
}

# Health check function
check_service_health() {
    local service="$1"
    local max_attempts="${2:-30}"
    local attempt=0
    
    log_info "Checking health of service: $service"
    
    while [[ $attempt -lt $max_attempts ]]; do
        if docker-compose -f "$DOCKER_COMPOSE_FILE" ps "$service" | grep -q "Up (healthy)"; then
            log_success "Service $service is healthy"
            return 0
        fi
        
        ((attempt++))
        log_info "Attempt $attempt/$max_attempts - waiting for $service to be healthy..."
        sleep 10
    done
    
    log_error "Service $service failed health check after $max_attempts attempts"
    return 1
}

# Deploy function
deploy() {
    log_info "Starting deployment to $ENVIRONMENT environment"
    log_info "Image tag: $IMAGE_TAG"
    log_info "Compose project: $COMPOSE_PROJECT_NAME"
    
    if [[ "$DRY_RUN" == "true" ]]; then
        log_warn "DRY RUN MODE - Commands will be printed but not executed"
    fi
    
    # Pull latest images
    log_info "Pulling latest Docker images..."
    if [[ "$DRY_RUN" != "true" ]]; then
        docker-compose -f "$DOCKER_COMPOSE_FILE" pull
    else
        echo "docker-compose -f $DOCKER_COMPOSE_FILE pull"
    fi
    
    # Start services
    log_info "Starting services..."
    if [[ "$DRY_RUN" != "true" ]]; then
        docker-compose -f "$DOCKER_COMPOSE_FILE" up -d
    else
        echo "docker-compose -f $DOCKER_COMPOSE_FILE up -d"
    fi
    
    # Wait for services to be ready
    if [[ "$DRY_RUN" != "true" ]]; then
        log_info "Waiting for services to be ready..."
        sleep 30
        
        # Check critical services
        check_service_health "gameengine" 30
        check_service_health "redis" 20
        check_service_health "postgres" 20
    fi
    
    log_success "Deployment completed successfully!"
}

# Rollback function
rollback() {
    local previous_tag="${3:-$(get_previous_tag)}"
    
    log_info "Rolling back deployment in $ENVIRONMENT environment"
    log_info "Rolling back to tag: $previous_tag"
    
    if [[ -z "$previous_tag" ]]; then
        log_error "No previous tag specified and unable to determine previous version"
        exit 1
    fi
    
    # Update image tag for rollback
    export DEPLOY_TAG="$previous_tag"
    
    # Redeploy with previous tag
    deploy
    
    log_success "Rollback completed successfully!"
}

# Get previous tag function (placeholder - would integrate with registry API)
get_previous_tag() {
    # This is a placeholder - in a real implementation, this would query the container registry
    # or deployment history to find the previous successful deployment tag
    echo "previous"
}

# Status check function
status() {
    log_info "Checking status of $ENVIRONMENT deployment"
    
    docker-compose -f "$DOCKER_COMPOSE_FILE" ps
    
    echo ""
    log_info "Service logs (last 50 lines):"
    docker-compose -f "$DOCKER_COMPOSE_FILE" logs --tail=50
}

# Stop function
stop() {
    log_info "Stopping $ENVIRONMENT deployment"
    
    if [[ "$DRY_RUN" != "true" ]]; then
        docker-compose -f "$DOCKER_COMPOSE_FILE" down
    else
        echo "docker-compose -f $DOCKER_COMPOSE_FILE down"
    fi
    
    log_success "Services stopped successfully!"
}

# Main function
main() {
    log_info "GameConsole Deployment Script"
    log_info "Environment: $ENVIRONMENT"
    log_info "Action: $ACTION"
    log_info "Image Tag: $IMAGE_TAG"
    echo ""
    
    check_prerequisites
    load_environment
    
    case "$ACTION" in
        "deploy")
            deploy
            ;;
        "rollback")
            rollback
            ;;
        "status")
            status
            ;;
        "stop")
            stop
            ;;
        *)
            log_error "Unknown action: $ACTION"
            log_error "Available actions: deploy, rollback, status, stop"
            exit 1
            ;;
    esac
}

# Usage information
if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    cat << EOF
GameConsole Deployment Script

Usage: $0 [ENVIRONMENT] [ACTION] [IMAGE_TAG]

ENVIRONMENT:
    development (default)
    staging
    production

ACTION:
    deploy (default)    - Deploy the application
    rollback           - Rollback to previous version
    status            - Check deployment status
    stop              - Stop all services

IMAGE_TAG:
    latest (default)   - Docker image tag to deploy

Environment Variables:
    DRY_RUN=true      - Print commands without executing them

Examples:
    $0                               # Deploy latest to development
    $0 staging deploy v1.2.3         # Deploy v1.2.3 to staging
    $0 production rollback           # Rollback production
    DRY_RUN=true $0 production deploy # Dry run production deployment

EOF
    exit 0
fi

# Run main function
main "$@"