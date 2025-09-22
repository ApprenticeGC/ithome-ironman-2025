#!/usr/bin/env python3
"""
Deployment Pipeline Automation Script
Part of RFC-012-02: Create Deployment Pipeline Automation

This script handles:
- Building and packaging GameConsole.* libraries as NuGet packages
- Publishing packages to GitHub Packages
- Creating GitHub releases with artifacts
"""

import argparse
import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional


class DeploymentAutomation:
    """Handles automated deployment pipeline operations."""

    def __init__(self, repo_root: Optional[str] = None):
        self.repo_root = Path(repo_root) if repo_root else Path(__file__).parent.parent.parent.parent
        self.dotnet_path = self.repo_root / "dotnet"
        self.build_path = self.repo_root / "build"
        self.packages_path = self.build_path / "packages"
        
        # GameConsole libraries to package (exclude test projects and TestLib)
        self.package_projects = [
            "GameConsole.Core.Abstractions",
            "GameConsole.Core.Registry", 
            "GameConsole.Engine.Core",
            "GameConsole.Audio.Core",
            "GameConsole.Graphics.Core",
            "GameConsole.Graphics.Services",
            "GameConsole.Input.Core",
            "GameConsole.Input.Services",
            "GameConsole.Plugins.Core",
            "GameConsole.Plugins.Lifecycle",
            "GameConsole.Configuration.Security"
        ]

    def run_command(self, cmd: List[str], cwd: Optional[Path] = None, capture_output: bool = False) -> subprocess.CompletedProcess:
        """Run a shell command with proper error handling."""
        try:
            result = subprocess.run(
                cmd,
                cwd=cwd or self.repo_root,
                capture_output=capture_output,
                text=True,
                encoding='utf-8',
                errors='replace'
            )
            if result.returncode != 0:
                print(f"Command failed: {' '.join(cmd)}")
                if capture_output:
                    print(f"stdout: {result.stdout}")
                    print(f"stderr: {result.stderr}")
                sys.exit(result.returncode)
            return result
        except Exception as e:
            print(f"Error running command {' '.join(cmd)}: {e}")
            sys.exit(1)

    def ensure_build_directory(self) -> None:
        """Ensure build directories exist."""
        self.packages_path.mkdir(parents=True, exist_ok=True)
        print(f"Build directory created: {self.packages_path}")

    def update_project_versions(self, version: str) -> None:
        """Update version information in project files."""
        print(f"Updating project versions to {version}")
        
        # Split version for assembly vs package versioning
        numeric_version = version.split('-')[0]  # Remove pre-release suffix for AssemblyVersion
        
        for project in self.package_projects:
            project_path = self.dotnet_path / project / f"{project}.csproj"
            if not project_path.exists():
                print(f"Warning: Project file not found: {project_path}")
                continue
                
            # Read current project file
            with open(project_path, 'r', encoding='utf-8', errors='replace') as f:
                content = f.read()
            
            # Add version properties if not present
            if '<Version>' not in content and '<PackageVersion>' not in content:
                # Find PropertyGroup and add version info
                property_group_end = content.find('</PropertyGroup>')
                if property_group_end != -1:
                    version_props = f"""    <Version>{version}</Version>
    <PackageVersion>{version}</PackageVersion>
    <AssemblyVersion>{numeric_version}</AssemblyVersion>
    <FileVersion>{numeric_version}</FileVersion>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <IsPackable>true</IsPackable>
"""
                    content = content[:property_group_end] + version_props + content[property_group_end:]
                    
                    with open(project_path, 'w', encoding='utf-8', errors='replace') as f:
                        f.write(content)
                    print(f"Updated version in {project}")

    def create_packages(self, version: str, configuration: str = "Release") -> List[Path]:
        """Create NuGet packages for all GameConsole projects."""
        self.ensure_build_directory()
        self.update_project_versions(version)
        
        print(f"Creating NuGet packages (version: {version}, configuration: {configuration})")
        created_packages = []
        
        for project in self.package_projects:
            project_path = self.dotnet_path / project
            if not project_path.exists():
                print(f"Warning: Project directory not found: {project_path}")
                continue
                
            print(f"Packaging {project}...")
            
            # Create package with explicit output directory
            # Use separate version properties to handle pre-release versions
            numeric_version = version.split('-')[0]  # Remove pre-release suffix for AssemblyVersion
            self.run_command([
                "dotnet", "pack", str(project_path),
                "--configuration", configuration,
                "--output", str(self.packages_path),
                "--verbosity", "normal",
                f"-p:PackageVersion={version}",
                f"-p:Version={version}",
                f"-p:AssemblyVersion={numeric_version}",
                f"-p:FileVersion={numeric_version}"
            ])
            
            # Find the created package
            package_pattern = f"{project}.{version}.nupkg"
            package_file = self.packages_path / package_pattern
            if package_file.exists():
                created_packages.append(package_file)
                print(f"Created package: {package_file}")
            else:
                print(f"Warning: Expected package not found: {package_file}")
        
        print(f"Successfully created {len(created_packages)} packages")
        return created_packages

    def publish_packages(self, packages: List[Path], token: str) -> None:
        """Publish packages to GitHub Packages."""
        if not packages:
            print("No packages to publish")
            return
            
        print(f"Publishing {len(packages)} packages to GitHub Packages...")
        
        # Get repository info for GitHub Packages
        repo_owner = os.getenv('GITHUB_REPOSITORY_OWNER', 'ApprenticeGC')
        nuget_source = f"https://nuget.pkg.github.com/{repo_owner}/index.json"
        
        for package in packages:
            print(f"Publishing {package.name}...")
            self.run_command([
                "dotnet", "nuget", "push", str(package),
                "--source", nuget_source,
                "--api-key", token,
                "--skip-duplicate"
            ])
            print(f"Published {package.name}")

    def create_github_release(self, version: str, token: str, packages: List[Path]) -> None:
        """Create GitHub release with package artifacts."""
        print(f"Creating GitHub release for version {version}")
        
        # Set GitHub token
        os.environ['GH_TOKEN'] = token
        
        # Check if release already exists
        repo = os.getenv('GITHUB_REPOSITORY', 'ApprenticeGC/ithome-ironman-2025')
        try:
            result = self.run_command([
                "gh", "release", "view", f"v{version}", "--repo", repo
            ], capture_output=True)
            print(f"Release v{version} already exists")
            return
        except:
            pass  # Release doesn't exist, continue creating
        
        # Create release
        release_notes = f"""# GameConsole Libraries v{version}

This release contains the following GameConsole NuGet packages:

"""
        for package in packages:
            release_notes += f"- {package.name}\n"
        
        release_notes += f"""
## Installation

Install packages using the .NET CLI:
```
dotnet add package GameConsole.Core.Abstractions --version {version}
dotnet add package GameConsole.Engine.Core --version {version}
# ... other packages
```

## Package Source

Packages are available on GitHub Packages:
```
dotnet nuget add source https://nuget.pkg.github.com/ApprenticeGC/index.json --name "GitHub"
```
"""
        
        # Create the release
        self.run_command([
            "gh", "release", "create", f"v{version}",
            "--repo", repo,
            "--title", f"GameConsole Libraries v{version}",
            "--notes", release_notes
        ])
        
        # Upload package artifacts
        if packages:
            print("Uploading package artifacts...")
            package_paths = [str(p) for p in packages]
            self.run_command([
                "gh", "release", "upload", f"v{version}",
                "--repo", repo
            ] + package_paths)
            
        print(f"Successfully created release v{version}")

    def run_action(self, action: str, **kwargs) -> None:
        """Execute the specified deployment action."""
        if action == "package":
            version = kwargs.get('version')
            configuration = kwargs.get('configuration', 'Release')
            if not version:
                raise ValueError("Version is required for package action")
            
            packages = self.create_packages(version, configuration)
            print(f"Package creation completed. Created {len(packages)} packages.")
            
        elif action == "publish":
            version = kwargs.get('version')
            token = kwargs.get('token')
            if not version or not token:
                raise ValueError("Version and token are required for publish action")
            
            # Find existing packages
            packages = list(self.packages_path.glob(f"*.{version}.nupkg"))
            if not packages:
                print("No packages found. Creating packages first...")
                packages = self.create_packages(version)
            
            self.publish_packages(packages, token)
            print("Package publishing completed.")
            
        elif action == "release":
            version = kwargs.get('version')
            token = kwargs.get('token')
            if not version or not token:
                raise ValueError("Version and token are required for release action")
            
            # Find existing packages
            packages = list(self.packages_path.glob(f"*.{version}.nupkg"))
            if not packages:
                print("No packages found. Creating packages first...")
                packages = self.create_packages(version)
            
            self.create_github_release(version, token, packages)
            print("GitHub release creation completed.")
            
        else:
            raise ValueError(f"Unknown action: {action}")


def main():
    parser = argparse.ArgumentParser(description="GameConsole Deployment Pipeline Automation")
    parser.add_argument("--action", required=True, 
                       choices=["package", "publish", "release"],
                       help="Deployment action to perform")
    parser.add_argument("--version", required=True,
                       help="Version to deploy (e.g., 1.0.0)")
    parser.add_argument("--configuration", default="Release",
                       help="Build configuration (default: Release)")
    parser.add_argument("--token", 
                       help="GitHub token for publishing/releases")
    
    args = parser.parse_args()
    
    try:
        automation = DeploymentAutomation()
        automation.run_action(
            action=args.action,
            version=args.version,
            configuration=args.configuration,
            token=args.token
        )
        print(f"Deployment action '{args.action}' completed successfully.")
        
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()