using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

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

    [Parameter("Container registry for pushing images")]
    readonly string Registry = "ghcr.io";

    [Parameter("Container image name")]
    readonly string ImageName = "gameconsole";

    [Parameter("Container image tag")]
    readonly string ImageTag = "latest";

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "dotnet";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(path => path.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
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
                .SetTreatWarningsAsErrors(true)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(SourceDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetVerbosity(DotNetVerbosity.normal));
        });

    Target Package => _ => _
        .DependsOn(Test)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(SourceDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetOutputDirectory(ArtifactsDirectory));
        });

    Target ContainerBuild => _ => _
        .DependsOn(Package)
        .Executes(() =>
        {
            DockerBuild(s => s
                .SetPath(RootDirectory)
                .SetFile(RootDirectory / "Dockerfile")
                .SetTag($"{ImageName}:{ImageTag}"));
        });

    Target ContainerPush => _ => _
        .DependsOn(ContainerBuild)
        .Executes(() =>
        {
            var fullImageName = $"{Registry}/{ImageName}:{ImageTag}";
            
            DockerTag(s => s
                .SetSourceImage($"{ImageName}:{ImageTag}")
                .SetTargetImage(fullImageName));
            
            DockerPush(s => s
                .SetName(fullImageName));
        });

    Target Deploy => _ => _
        .DependsOn(ContainerPush);

}
