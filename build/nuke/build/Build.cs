using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Version to use for packages")]
    readonly string Version = "1.0.0";

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "dotnet";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
            DotNetClean(s => s
                .SetProject(SourceDirectory)
                .SetConfiguration(Configuration));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(SourceDirectory));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(SourceDirectory)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetTreatWarningsAsErrors(true));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(SourceDirectory)
                .SetConfiguration(Configuration)
                .SetNoBuild(true)
                .SetLogger("trx")
                .SetResultsDirectory(ArtifactsDirectory / "test-results"));
        });

    Target Package => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            // Get all GameConsole library projects (exclude test projects)
            var libraryProjects = Solution.Projects
                .Where(p => p.Name.StartsWith("GameConsole.") && 
                           !p.Name.EndsWith(".Tests") && 
                           !p.Name.Contains("TestLib"))
                .ToList();

            foreach (var project in libraryProjects)
            {
                DotNetPack(s => s
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .SetOutputDirectory(PackagesDirectory)
                    .SetProperty("PackageVersion", Version)
                    .SetProperty("Version", Version)
                    .SetNoBuild(false) // Need to build for packaging
                    .SetNoRestore(false));
            }

            Log.Information($"Created {libraryProjects.Count} packages in {PackagesDirectory}");
        });

    Target Publish => _ => _
        .DependsOn(Package)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            var packages = PackagesDirectory.GlobFiles("*.nupkg");
            
            foreach (var package in packages)
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource("https://nuget.pkg.github.com/ApprenticeGC/index.json")
                    .SetApiKey(Environment.GetEnvironmentVariable("GITHUB_TOKEN"))
                    .SetSkipDuplicate(true));
            }

            Log.Information($"Published {packages.Count} packages to GitHub Packages");
        });
}
