#!/bin/bash

# GameConsole Deployment Script
# Usage: ./deploy.sh <environment> <package_path> [options]

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Default values
ENVIRONMENT=""
PACKAGE_PATH=""
SKIP_HEALTH_CHECK=false
ROLLBACK_ON_FAILURE=true
DEPLOYMENT_TIMEOUT=300

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

# Show usage
show_help() {
    cat << EOF
GameConsole Deployment Script

Usage: $0 <environment> <package_path> [options]

Arguments:
    environment     Target environment (staging|production)
    package_path    Path to deployment package (.tar.gz)

Options:
    --skip-health-check    Skip post-deployment health checks
    --no-rollback         Don't rollback on failure
    --timeout <seconds>   Deployment timeout (default: 300)
    --help               Show this help message

Examples:
    $0 staging gameconsole-staging-abc123.tar.gz
    $0 production gameconsole-production-v1.0.0.tar.gz --timeout 600
EOF
}

# Parse command line arguments
parse_args() {
    if [[ $# -lt 2 ]]; then
        show_help
        exit 1
    fi

    ENVIRONMENT="$1"
    PACKAGE_PATH="$2"
    shift 2

    while [[ $# -gt 0 ]]; do
        case $1 in
            --skip-health-check)
                SKIP_HEALTH_CHECK=true
                shift
                ;;
            --no-rollback)
                ROLLBACK_ON_FAILURE=false
                shift
                ;;
            --timeout)
                DEPLOYMENT_TIMEOUT="$2"
                shift 2
                ;;
            --help)
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
    case "$ENVIRONMENT" in
        staging|production)
            log_info "Deploying to $ENVIRONMENT environment"
            ;;
        *)
            log_error "Invalid environment: $ENVIRONMENT. Must be 'staging' or 'production'"
            exit 1
            ;;
    esac
}

# Validate deployment package
validate_package() {
    if [[ ! -f "$PACKAGE_PATH" ]]; then
        log_error "Package file not found: $PACKAGE_PATH"
        exit 1
    fi

    if [[ ! "$PACKAGE_PATH" =~ \.tar\.gz$ ]]; then
        log_error "Package must be a .tar.gz file"
        exit 1
    fi

    log_info "Package validated: $PACKAGE_PATH"
}

# Extract deployment package
extract_package() {
    local temp_dir
    temp_dir=$(mktemp -d)
    
    log_info "Extracting package to temporary directory: $temp_dir"
    tar -xzf "$PACKAGE_PATH" -C "$temp_dir"
    
    echo "$temp_dir"
}

# Deploy to environment
deploy_application() {
    local extract_dir="$1"
    
    log_info "Starting deployment to $ENVIRONMENT"
    log_info "Deployment started at: $(date)"
    
    # Simulate deployment steps
    log_info "📦 Preparing deployment package"
    sleep 1
    
    log_info "🔄 Stopping existing services"
    sleep 1
    
    log_info "⬆️  Uploading application files"
    sleep 2
    
    log_info "⚙️  Updating configuration for $ENVIRONMENT"
    sleep 1
    
    log_info "🚀 Starting services"
    sleep 2
    
    log_success "Deployment completed successfully"
}

# Health check
run_health_check() {
    if [[ "$SKIP_HEALTH_CHECK" == true ]]; then
        log_warning "Health check skipped"
        return 0
    fi

    log_info "🏥 Running health checks"
    
    # Simulate health checks
    log_info "Checking application startup..."
    sleep 1
    
    log_info "Verifying service endpoints..."
    sleep 1
    
    log_info "Testing core functionality..."
    sleep 1
    
    log_success "Health check passed"
}

# Cleanup temporary files
cleanup() {
    local extract_dir="$1"
    if [[ -n "$extract_dir" && -d "$extract_dir" ]]; then
        log_info "Cleaning up temporary files"
        rm -rf "$extract_dir"
    fi
}

# Main deployment function
main() {
    parse_args "$@"
    validate_environment
    validate_package
    
    local extract_dir
    extract_dir=$(extract_package)
    
    # Set up cleanup trap
    trap "cleanup '$extract_dir'" EXIT
    
    # Deploy application
    deploy_application "$extract_dir"
    
    # Run health checks
    run_health_check
    
    log_success "🎉 Deployment to $ENVIRONMENT completed successfully!"
    log_info "📊 Deployment summary:"
    log_info "   Environment: $ENVIRONMENT"
    log_info "   Package: $(basename "$PACKAGE_PATH")"
    log_info "   Time: $(date)"
}

# Run main function
main "$@"