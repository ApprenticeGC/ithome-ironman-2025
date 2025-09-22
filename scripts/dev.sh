#!/bin/bash
# GameConsole Local Development Script  
# RFC-012-02: Deployment Pipeline Automation

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

show_help() {
    cat << EOF
GameConsole Local Development Script

Usage: $0 [COMMAND]

Commands:
    build       Build the Docker image
    run         Run the application with Docker Compose
    stop        Stop the running containers
    clean       Stop containers and remove images
    logs        Show application logs
    test        Run tests locally
    shell       Open shell in running container
    help        Show this help message

Examples:
    $0 build
    $0 run
    $0 logs
    $0 clean
EOF
}

build_image() {
    log_info "Building GameConsole Docker image..."
    cd "$PROJECT_ROOT"
    docker build -t gameconsole:latest .
    log_success "Docker image built successfully"
}

run_app() {
    log_info "Starting GameConsole with Docker Compose..."
    cd "$PROJECT_ROOT"
    docker-compose up -d
    log_success "GameConsole is running!"
    log_info "Access the application at: http://localhost:8080"
    log_info "Use '$0 logs' to view application logs"
}

stop_app() {
    log_info "Stopping GameConsole containers..."
    cd "$PROJECT_ROOT"
    docker-compose down
    log_success "Containers stopped"
}

clean_up() {
    log_info "Cleaning up containers and images..."
    cd "$PROJECT_ROOT"
    docker-compose down --rmi all --volumes --remove-orphans
    log_success "Cleanup completed"
}

show_logs() {
    log_info "Showing GameConsole logs..."
    cd "$PROJECT_ROOT"
    docker-compose logs -f gameconsole
}

run_tests() {
    log_info "Running tests locally..."
    cd "$PROJECT_ROOT"
    
    # Run tests with .NET CLI
    dotnet test ./dotnet --verbosity normal
    
    log_success "Tests completed"
}

open_shell() {
    log_info "Opening shell in GameConsole container..."
    cd "$PROJECT_ROOT"
    docker-compose exec gameconsole /bin/bash || {
        log_error "Container not running. Start it with '$0 run' first"
        exit 1
    }
}

main() {
    case "${1:-help}" in
        build)
            build_image
            ;;
        run)
            run_app
            ;;
        stop)
            stop_app
            ;;
        clean)
            clean_up
            ;;
        logs)
            show_logs
            ;;
        test)
            run_tests
            ;;
        shell)
            open_shell
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            log_error "Unknown command: $1"
            show_help
            exit 1
            ;;
    esac
}

main "$@"