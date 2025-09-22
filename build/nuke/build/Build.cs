using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using Serilog;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / ".." / ".." / "dotnet";
    AbsolutePath ArtifactsDirectory => RootDirectory / ".." / ".." / "artifacts";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    // Simple versioning - can be enhanced later with GitVersion
    string Version => "1.0.0";
    string AssemblyVersion => $"{Version}.0";
    string FileVersion => $"{Version}.0";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            var binDirs = SourceDirectory.GlobDirectories("**/bin");
            var objDirs = SourceDirectory.GlobDirectories("**/obj");
            foreach (var dir in binDirs.Concat(objDirs))
            {
                dir.DeleteDirectory();
            }
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            ProcessTasks.StartProcess("dotnet", $"restore {SourceDirectory / "TestSolution.sln"}")
                .AssertZeroExitCode();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var arguments = new[]
            {
                "build",
                (SourceDirectory / "TestSolution.sln").ToString(),
                $"--configuration {Configuration}",
                "--no-restore",
                "--property TreatWarningsAsErrors=true",
                $"--property AssemblyVersion={AssemblyVersion}",
                $"--property FileVersion={FileVersion}",
                $"--property InformationalVersion={Version}"
            };

            ProcessTasks.StartProcess("dotnet", string.Join(" ", arguments))
                .AssertZeroExitCode();
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var arguments = new[]
            {
                "test",
                (SourceDirectory / "TestSolution.sln").ToString(),
                $"--configuration {Configuration}",
                "--no-build",
                "--no-restore",
                "--verbosity normal"
            };

            ProcessTasks.StartProcess("dotnet", string.Join(" ", arguments))
                .AssertZeroExitCode();
        });

    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            var arguments = new[]
            {
                "pack",
                (SourceDirectory / "TestSolution.sln").ToString(),
                $"--configuration {Configuration}",
                $"--output {PackagesDirectory}",
                $"--property PackageVersion={Version}",
                "--no-build",
                "--no-restore"
            };

            ProcessTasks.StartProcess("dotnet", string.Join(" ", arguments))
                .AssertZeroExitCode();

            Log.Information("NuGet packages created:");
            PackagesDirectory.GlobFiles("*.nupkg")
                .ForEach(package => Log.Information($"  - {package}"));
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            // Placeholder for publishing packages to NuGet feed
            // This would be implemented based on the target feed (NuGet.org, Azure DevOps, GitHub packages)
            Log.Information("Publish target - packages ready for deployment");
            PackagesDirectory.GlobFiles("*.nupkg")
                .ForEach(package => Log.Information($"Package ready: {package}"));
        });

    Target Deploy => _ => _
        .DependsOn(Publish)
        .OnlyWhenStatic(() => IsServerBuild)
        .Executes(() =>
        {
            Log.Information("Deployment pipeline completed successfully");
        });

}
