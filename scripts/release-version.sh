#!/bin/bash
# Version management script for GameConsole releases

set -e

ACTION="${1:-help}"
VERSION="${2:-}"

show_help() {
    echo "GameConsole Release Version Manager"
    echo ""
    echo "Usage: $0 <action> [version]"
    echo ""
    echo "Actions:"
    echo "  help            Show this help"
    echo "  next-patch      Calculate next patch version"
    echo "  next-minor      Calculate next minor version"
    echo "  next-major      Calculate next major version"
    echo "  validate <ver>  Validate version format"
    echo "  tag <ver>       Create and push version tag"
    echo ""
    echo "Examples:"
    echo "  $0 next-patch"
    echo "  $0 validate 1.2.3"
    echo "  $0 tag 1.0.0"
}

get_latest_tag() {
    git describe --tags --abbrev=0 2>/dev/null | sed 's/^v//' || echo "0.0.0"
}

validate_version() {
    local ver="$1"
    if [[ ! "$ver" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?$ ]]; then
        echo "❌ Invalid version format: $ver"
        echo "Expected: x.y.z or x.y.z-suffix"
        return 1
    fi
    echo "✅ Valid version: $ver"
    return 0
}

increment_version() {
    local version="$1"
    local part="$2"
    
    # Remove any pre-release suffix
    version=$(echo "$version" | cut -d'-' -f1)
    
    IFS='.' read -r -a parts <<< "$version"
    major="${parts[0]:-0}"
    minor="${parts[1]:-0}"
    patch="${parts[2]:-0}"
    
    case "$part" in
        "major")
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        "minor")
            minor=$((minor + 1))
            patch=0
            ;;
        "patch")
            patch=$((patch + 1))
            ;;
        *)
            echo "Unknown version part: $part"
            return 1
            ;;
    esac
    
    echo "$major.$minor.$patch"
}

create_tag() {
    local version="$1"
    local tag="v$version"
    
    if ! validate_version "$version"; then
        return 1
    fi
    
    if git rev-parse "$tag" >/dev/null 2>&1; then
        echo "❌ Tag $tag already exists"
        return 1
    fi
    
    echo "Creating tag: $tag"
    git tag -a "$tag" -m "Release $version"
    
    echo "Pushing tag to origin..."
    git push origin "$tag"
    
    echo "✅ Tag $tag created and pushed successfully"
    echo "🚀 Release workflow should start automatically"
}

case "$ACTION" in
    "help")
        show_help
        ;;
    "next-patch"|"next-minor"|"next-major")
        current=$(get_latest_tag)
        part=$(echo "$ACTION" | cut -d'-' -f2)
        next=$(increment_version "$current" "$part")
        echo "Current version: $current"
        echo "Next $part version: $next"
        ;;
    "validate")
        if [ -z "$VERSION" ]; then
            echo "❌ Version required for validate action"
            echo "Usage: $0 validate <version>"
            exit 1
        fi
        validate_version "$VERSION"
        ;;
    "tag")
        if [ -z "$VERSION" ]; then
            echo "❌ Version required for tag action"
            echo "Usage: $0 tag <version>"
            exit 1
        fi
        create_tag "$VERSION"
        ;;
    *)
        echo "❌ Unknown action: $ACTION"
        show_help
        exit 1
        ;;
esac