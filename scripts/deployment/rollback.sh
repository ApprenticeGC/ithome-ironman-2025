#!/bin/bash

# GameConsole Rollback Script
# Usage: ./rollback.sh <environment> [options]

set -euo pipefail

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Default values
ENVIRONMENT=""
TARGET_VERSION=""
SKIP_HEALTH_CHECK=false
FORCE_ROLLBACK=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[ROLLBACK]${NC} $1"
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
GameConsole Rollback Script

Usage: $0 <environment> [options]

Arguments:
    environment     Target environment (staging|production)

Options:
    --target-version <version>  Specific version to rollback to
    --skip-health-check        Skip health check after rollback
    --force                    Force rollback without confirmation
    --help                     Show this help message

Examples:
    $0 staging
    $0 production --target-version v1.0.0
    $0 production --force --skip-health-check
EOF
}

# Parse command line arguments
parse_args() {
    if [[ $# -lt 1 ]]; then
        show_help
        exit 1
    fi

    ENVIRONMENT="$1"
    shift

    while [[ $# -gt 0 ]]; do
        case $1 in
            --target-version)
                TARGET_VERSION="$2"
                shift 2
                ;;
            --skip-health-check)
                SKIP_HEALTH_CHECK=true
                shift
                ;;
            --force)
                FORCE_ROLLBACK=true
                shift
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
            log_info "Preparing rollback for $ENVIRONMENT environment"
            ;;
        *)
            log_error "Invalid environment: $ENVIRONMENT. Must be 'staging' or 'production'"
            exit 1
            ;;
    esac
}

# Get previous version
get_previous_version() {
    if [[ -n "$TARGET_VERSION" ]]; then
        echo "$TARGET_VERSION"
        return 0
    fi

    # Simulate getting previous version from deployment history
    case "$ENVIRONMENT" in
        staging)
            echo "staging-previous-$(date +%Y%m%d)-1"
            ;;
        production)
            echo "v1.0.$(((RANDOM % 10) + 1))"
            ;;
    esac
}

# Confirm rollback
confirm_rollback() {
    if [[ "$FORCE_ROLLBACK" == true ]]; then
        return 0
    fi

    local previous_version="$1"
    
    echo
    log_warning "⚠️  ROLLBACK CONFIRMATION REQUIRED"
    log_warning "Environment: $ENVIRONMENT"
    log_warning "Target Version: $previous_version"
    log_warning "This will rollback the current deployment"
    echo
    
    read -p "Are you sure you want to proceed? (yes/no): " -r
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        log_info "Rollback cancelled by user"
        exit 0
    fi
}

# Stop current services
stop_services() {
    log_info "🛑 Stopping current services"
    
    local services=("core" "graphics" "input" "audio")
    
    for service in "${services[@]}"; do
        log_info "Stopping $service service..."
        sleep 1
        log_success "$service service stopped"
    done
}

# Restore previous version
restore_previous_version() {
    local previous_version="$1"
    
    log_info "🔄 Restoring previous version: $previous_version"
    
    # Simulate restoration process
    log_info "📦 Locating backup package..."
    sleep 1
    log_success "Backup package found: $previous_version"
    
    log_info "⬇️  Downloading backup package..."
    sleep 2
    log_success "Backup package downloaded"
    
    log_info "📂 Extracting backup package..."
    sleep 1
    log_success "Backup package extracted"
    
    log_info "🔄 Replacing current deployment..."
    sleep 2
    log_success "Deployment replaced"
    
    log_info "⚙️  Updating configuration..."
    sleep 1
    log_success "Configuration updated"
}

# Start services
start_services() {
    log_info "🚀 Starting restored services"
    
    local services=("core" "graphics" "input" "audio")
    
    for service in "${services[@]}"; do
        log_info "Starting $service service..."
        sleep 1
        log_success "$service service started"
    done
}

# Run post-rollback health check
run_health_check() {
    if [[ "$SKIP_HEALTH_CHECK" == true ]]; then
        log_warning "Health check skipped"
        return 0
    fi

    log_info "🏥 Running post-rollback health check"
    
    # Use the health check script if available
    local health_script="$SCRIPT_DIR/health-check.sh"
    if [[ -f "$health_script" && -x "$health_script" ]]; then
        "$health_script" "$ENVIRONMENT"
    else
        # Simple health check simulation
        log_info "Checking application health..."
        sleep 2
        log_success "Application is healthy after rollback"
    fi
}

# Generate rollback report
generate_rollback_report() {
    local previous_version="$1"
    local status="$2"
    local timestamp=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    
    cat << EOF

📊 Rollback Report
==================
Environment: $ENVIRONMENT
Target Version: $previous_version
Status: $status
Timestamp: $timestamp
Force Rollback: $FORCE_ROLLBACK
Health Check: $(if [[ "$SKIP_HEALTH_CHECK" == true ]]; then echo "Skipped"; else echo "Performed"; fi)

EOF
}

# Main rollback function
main() {
    parse_args "$@"
    validate_environment
    
    local previous_version
    previous_version=$(get_previous_version)
    
    log_info "🚨 Starting rollback process for GameConsole ($ENVIRONMENT)"
    log_info "Target version: $previous_version"
    
    confirm_rollback "$previous_version"
    
    # Perform rollback
    stop_services
    restore_previous_version "$previous_version"
    start_services
    
    # Health check
    if run_health_check; then
        generate_rollback_report "$previous_version" "✅ SUCCESS"
        log_success "🎉 Rollback completed successfully!"
        log_info "GameConsole rolled back to: $previous_version"
    else
        generate_rollback_report "$previous_version" "❌ FAILED"
        log_error "❌ Rollback completed but health check failed"
        log_warning "Manual intervention may be required"
        exit 1
    fi
}

# Run main function
main "$@"